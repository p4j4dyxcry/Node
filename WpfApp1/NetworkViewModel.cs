﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Livet;
using Reactive.Bindings;
using TsGui.Foundation.Property;
using TsGui.Operation;
using TsNode.Controls;
using TsNode.Interface;

namespace WpfApp1
{
    public class ConnectionCreator
    {
        public ReactiveCommand<CompletedCreateConnectionEventArgs> ConnectCommand { get; }
        public ReactiveCommand<StartCreateConnectionEventArgs> StartNewConnectionCommand { get; }

        public ConnectionCreator(ObservableCollection<IConnectionDataContext> connecions , IOperationController @operator)
        {
            var removeOperation = Operation.Empty;

            //! コネクション接続完了コマンド
            ConnectCommand = new ReactiveCommand<CompletedCreateConnectionEventArgs>();
            ConnectCommand.Subscribe((e) =>
            {
                var operation = make_remove_duplication_plugs_operation(new[]
                    {
                        e.ConnectionDataContext.DestPlug,
                        e.ConnectionDataContext.SourcePlug
                    })
                    .CombineOperations(connecions.ToAddOperation(e.ConnectionDataContext))
                    .ToCompositeOperation();

                operation.Messaage = "コネクション接続";

                if (removeOperation.IsEmpty())
                    operation.ExecuteTo(@operator);
                else
                    operation.ExecuteAndCombineTop(@operator);
            });

            //! コネクション接続開始コマンド
            StartNewConnectionCommand = new ReactiveCommand<StartCreateConnectionEventArgs>();
            StartNewConnectionCommand.Subscribe((e) =>
            {
                removeOperation = make_remove_duplication_plugs_operation(e.SenderPlugs);

                if (removeOperation.IsNotEmpty())
                    removeOperation.ExecuteTo(@operator);

            });

            //! コネクション削除
            IOperation make_remove_duplication_plugs_operation(IPlugDataContext[] plugs)
            {
                var removeConnections = connecions.Where(x => plugs?.Contains(x.DestPlug) is true ||
                                                              plugs?.Contains(x.SourcePlug) is true).ToArray();

                if (removeConnections.Length is 0)
                    return Operation.Empty;

                var operation = connecions
                    .ToRemoveRangeOperation(removeConnections);
                operation.Messaage = "コノクションの削除";
                return operation;
            }
        }
    }

    public sealed class NetworkViewModel : ViewModel
    {
        //! ノード
        public ObservableCollection<INodeDataContext> Nodes { get; set; }

        //! コネクション
        public ObservableCollection<IConnectionDataContext> Connections { get; set; }

        //! コネクションドラッグ完了
        public ICommand ConnectCommand => _connectionCreator.ConnectCommand;

        //! コネクションドラッグ開始
        public ICommand StartNewConnectionCommand => _connectionCreator.StartNewConnectionCommand;

        //! 選択変更
        public ReactiveCommand<SelectionChangedEventArgs> SelectionChangedCommand { get; }

        //! ノード移動
        public ReactiveCommand<CompletedMoveNodeEventArgs> NodeMoveCommand { get; }

        //! 選択中の　ノード / コネクション　のプロパティ一覧
        private ObservableCollection<IProperty> _properties;

        public ObservableCollection<IProperty> Properties
        {
            get => _properties;
            set => RaisePropertyChangedIfSet(ref _properties, value);
        }

        private readonly IOperationController _operationController;
        private ConnectionCreator _connectionCreator;

        private ObservableCollection<INodeDataContext> CreateNodes( int count )
        {
             var nodes = new List<INodeDataContext>();

            var rand = new Random();
            foreach(var _ in Enumerable.Range(0, count))
            {
                var node = new NodeViewModel2(_operationController) { X = rand.NextDouble() * 1024 - 128, Y = rand.NextDouble() * 1024 - 128 };
                node.InputPlugs.Add(new PlugViewModel(_operationController));
                node.InputPlugs.Add(new PlugViewModel2(_operationController));
                node.OutputPlugs.Add(new PlugViewModel3(_operationController));
                node.OutputPlugs.Add(new PlugViewModel3(_operationController));
                nodes.Add(node);
            }
            return new ObservableCollection<INodeDataContext>(nodes);
        }

        public NetworkViewModel(IOperationController operationController)
        {
            _operationController = operationController;

            Nodes = CreateNodes(30);

            Connections = new ObservableCollection<IConnectionDataContext>();

            _connectionCreator = new ConnectionCreator(Connections,_operationController);

            NodeMoveCommand = new ReactiveCommand<CompletedMoveNodeEventArgs>();
            NodeMoveCommand.Subscribe(e =>
            {
                if (e.InitialNodePoints.Count is 0)
                    return;

                var operationBuilder = new OperationBuilder();
                var operation = operationBuilder.MakeFromAction(x =>
                    {
                        foreach (var keyValuePair in x)
                        {
                            keyValuePair.Key.X = keyValuePair.Value.X;
                            keyValuePair.Key.Y = keyValuePair.Value.Y;
                        }
                    },e.CompletedNodePoints, e.InitialNodePoints)
                    .Message("ノード移動")
                    .Build();
                _operationController.Push(operation);
            });

            SelectionChangedCommand = new ReactiveCommand<SelectionChangedEventArgs>();
            SelectionChangedCommand.Subscribe(e =>
            {
                bool[] selected = e.ChangedItems.Select(x => x.IsSelected).ToArray();

                var operationBuilder = new OperationBuilder();
                operationBuilder.MakeFromAction(
                    () =>
                    {
                        foreach(var i in Enumerable.Range(0, selected.Length))
                            e.ChangedItems[i].IsSelected = selected[i];
                    },
                    () =>
                    {
                        foreach (var i in Enumerable.Range(0, selected.Length))
                            e.ChangedItems[i].IsSelected = !selected[i];
                    })
                    .PostEvent(() => Properties = GenerateProperties(Nodes.Where(x => x.IsSelected)))
                    .Message("選択変更")
                    .Build()
                    .PushTo(_operationController);
                Properties = GenerateProperties(Nodes.Where(x => x.IsSelected));
            });
        }

        public ObservableCollection<IProperty> GenerateProperties(IEnumerable<ISelectable> items)
        {
            var selectables = items as ISelectable[] ?? items.ToArray();
            if (selectables.Any() is false)
            {
                return Enumerable.Empty<IProperty>().ToObservableCollection();
            }

            return new ReflectionPropertyBuilder(selectables.First())
                .GenerateProperties()
                .OperationController(_operationController)
                .Build()
                .ToObservableCollection();
        }
    }

}
