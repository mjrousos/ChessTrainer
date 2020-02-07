using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MjrChess.Trainer.BlazorExtensions
{
    public class MaterialDesignComponentBase : OwningComponentBase
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

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
