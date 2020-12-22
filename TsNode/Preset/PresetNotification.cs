﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TsNode.Preset
{
    /// <summary>
    /// 最低限実装の通知オブジェクト
    /// </summary>
    public class PresetNotification : INotifyPropertyChanged
    {
        protected bool RaisePropertyChangedIfSet<T>(ref T source,T value, [CallerMemberName] string propertyName = null)
        {
            return RaisePropertiesChangedIfSet(ref source, value, propertyName);
        }
        
        protected bool SetProperty<T>(ref T source,T value, [CallerMemberName] string propertyName = null)
        {
            return RaisePropertiesChangedIfSet(ref source, value, propertyName);
        }

        protected bool RaisePropertiesChangedIfSet<T>(ref T source,T value, params string[] properties)
        {
            if (EqualityComparer<T>.Default.Equals(source, value))
                return false;
            source = value;

            foreach (var property in properties)
                RaisePropertyChanged(property);

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
