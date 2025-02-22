﻿using Microsoft.AspNetCore.Components.Web;

namespace BlazorComponent
{
    public interface ISelectable<TValue> : IInput<TValue>, IRippleable
    {
        bool IsDisabled { get; }

        Dictionary<string, object> InputAttrs { get; }

        bool IsActive { get; }

        Task HandleOnBlur(FocusEventArgs args);

        Task HandleOnChange();

        Task HandleOnFocus(FocusEventArgs args);

        Task HandleOnKeyDown(KeyboardEventArgs args);
    }
}
