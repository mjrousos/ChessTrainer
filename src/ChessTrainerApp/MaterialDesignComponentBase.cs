using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ChessTrainerApp
{
    public class MaterialDesignComponentBase : ComponentBase
    {
        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await OnPageLoad();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task OnPageLoad()
        {
            var ret = await JSRuntime.InvokeAsync<string>("AttachMDC");
        }
    }
}
