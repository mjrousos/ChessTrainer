using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ChessTrainerApp.Test
{
    public class MainCommandButtonsTests : BunitContext
    {
        [Fact]
        public void NavigateToSource_NavigatesToGitHubRepo()
        {
            var cut = Render<MjrChess.Trainer.Components.MainCommandButtons>();

            cut.Find("button").Click();

            var navManager = Services.GetRequiredService<BunitNavigationManager>();
            Assert.Equal("https://github.com/mjrousos/ChessTrainer", navManager.Uri);
        }

        [Fact]
        public void SourceButton_HasCorrectAriaLabel()
        {
            var cut = Render<MjrChess.Trainer.Components.MainCommandButtons>();

            var button = cut.Find("button");

            Assert.Equal("Source on GitHub", button.GetAttribute("aria-label"));
        }
    }
}
