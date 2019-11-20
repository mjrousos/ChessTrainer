﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ChessTrainerApp.BlazorExtensions
{
    public class MaterialDesignComponentBase : ComponentBase
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await AttachMDCAsync();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task AttachMDCAsync()
        {
            await JSRuntime.InvokeVoidAsync("attachMDC");
        }
    }
}