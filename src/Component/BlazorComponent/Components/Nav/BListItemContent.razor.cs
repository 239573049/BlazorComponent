﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace BlazorComponent
{
    public abstract partial class BListItemContent
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
