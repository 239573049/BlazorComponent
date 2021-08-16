﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorComponent
{
    public interface ITreeview<TItem, TKey> : IAbstractComponent
    {
        bool OpenAll { get; }

        void AddNode(ITreeviewNode<TItem, TKey> node);

        void UpdateActive(TKey key);

        void UpdateSelected(TKey key);

        void UpdateOpen(TKey key);

        bool IsSelected(TKey key);

        bool IsIndeterminate(TKey key);

        bool IsActive(TKey key);

        bool IsOpen(TKey key);

        bool IsExcluded(TKey key);

        Task EmitActiveAsync();

        Task EmitOpenAsync();
        Task EmitSelectedAsync();
    }
}
