﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorComponent
{
    public partial class BTreeviewNode<TItem, TKey>
    {
        [Parameter]
        public TItem Item { get; set; }

        [Parameter]
        public bool OpenOnClick { get; set; }

        [Parameter]
        public Func<TItem, List<TItem>> ItemChildren { get; set; }

        [Parameter]
        public bool Activatable { get; set; }

        [Parameter]
        public Func<TItem, bool> ItemDisabled { get; set; }

        [Parameter]
        public RenderFragment<TreeviewItem<TItem>> PrependContent { get; set; }

        [Parameter]
        public RenderFragment<TreeviewItem<TItem>> LabelContent { get; set; }

        [Parameter]
        public EventCallback<TItem>? LoadChildren { get; set; }

        [Parameter]
        public Func<TItem, string> ItemText { get; set; }

        [Parameter]
        public bool ParentIsDisabled { get; set; }

        [Parameter]
        public RenderFragment<TreeviewItem<TItem>> AppendContent { get; set; }

        [Parameter]
        public bool Selectable { get; set; }

        [Parameter]
        public string IndeterminateIcon { get; set; }

        [Parameter]
        public string OnIcon { get; set; }

        [Parameter]
        public string OffIcon { get; set; }

        [Parameter]
        public SelectionType SelectionType { get; set; }

        [Parameter]
        public string LoadingIcon { get; set; }

        [Parameter]
        public string ExpandIcon { get; set; }

        [Parameter]
        public int Level { get; set; }

        [CascadingParameter]
        public ITreeview<TItem, TKey> Treeview { get; set; }

        [Parameter]
        public Func<TItem, TKey> ItemKey { get; set; }

        public List<TItem> ComputedChildren
        {
            get
            {
                if (Children == null)
                {
                    return null;
                }

                return Children.Where(r => !Treeview.IsExcluded(ItemKey(r))).ToList();
            }
        }

        //TODO:Excluded
        public List<TItem> Children => ItemChildren?.Invoke(Item);

        //TODO:load
        public bool HasChildren => Children != null && (Children.Count > 0 || LoadChildren != null);

        public bool Disabled => (ItemDisabled != null && ItemDisabled.Invoke(Item)) || (ParentIsDisabled && SelectionType == SelectionType.Leaf);

        public bool IsActive => Key != null && Treeview.IsActive(Key);

        public bool IsIndeterminate => Treeview.IsIndeterminate(Key);

        public bool IsLeaf => Children != null;

        public bool IsSelected => Treeview.IsSelected(Key);

        public string Text => ItemText?.Invoke(Item);

        public string ComputedIcon
        {
            get
            {
                if (IsIndeterminate)
                {
                    return IndeterminateIcon;
                }
                else if (IsSelected)
                {
                    return OnIcon;
                }

                return OffIcon;
            }
        }

        public bool IsLoading { get; set; }

        public bool IsOpen => Treeview.IsOpen(Key);

        public TKey Key => ItemKey.Invoke(Item);

        public async Task HandleOnClick(MouseEventArgs args)
        {
            if (OpenOnClick && HasChildren)
            {
                await CheckChildrenAsync();
                await OpenAsync();
            }
            else if (Activatable && !Disabled)
            {
                Treeview.UpdateActive(Key, !IsActive);
                await Treeview.EmitActiveAsync();
            }
        }

        private bool _hasLoaded;

        public async Task CheckChildrenAsync()
        {
            if (Children == null || Children.Any() || LoadChildren == null || _hasLoaded) return;

            IsLoading = true;

            try
            {
                await LoadChildren.Value.InvokeAsync(Item);
            }
            finally
            {
                IsLoading = false;
                _hasLoaded = true;
            }
        }

        public async Task OpenAsync()
        {
            Treeview.UpdateOpen(Key);
            await Treeview.EmitOpenAsync();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            Treeview.AddNode(this);
        }

        protected override void OnParametersSet()
        {
            if (ItemKey == null)
            {
                throw new ArgumentNullException(nameof(ItemKey));
            }
        }
    }
}