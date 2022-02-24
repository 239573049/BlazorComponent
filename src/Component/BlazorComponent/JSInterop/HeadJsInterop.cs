﻿using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorComponent
{
    public class HeadJsInterop
    {
        private readonly IJSRuntime _jsRuntime;

        public HeadJsInterop(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// The insertAdjacentHTML() method of the Element interface parses the specified text as HTML or XML and inserts the resulting nodes into the DOM tree at a specified position.
        /// <see cref="https://developer.mozilla.org/en-US/docs/Web/API/Element/insertAdjacentHTML"/>
        /// </summary>
        /// <param name="position"></param>
        /// <param name="text"></param>
        public void InsertAdjacentHTML(string position, string text)
        {
            _ = _jsRuntime.InvokeAsync<string>(JsInteropConstants.InsertAdjacentHTML, position, text);
        }
    }
}
