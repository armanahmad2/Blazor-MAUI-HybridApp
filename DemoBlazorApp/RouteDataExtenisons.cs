

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using SharedRazorClassLibrary;
using System.Collections.Concurrent;
using System.Reflection;

namespace DemoBlazorApp
{
    public static class RouteDataExtensions
    {
        private static readonly ConcurrentDictionary<Type, bool> s_componentInteractivityCache = new();

        public static Microsoft.AspNetCore.Components.RouteData WithAllowedInteractivity(this Microsoft.AspNetCore.Components.RouteData routeData, IComponentRenderMode interactiveRenderMode)
        {
            var isPageInteractive = s_componentInteractivityCache.GetOrAdd(routeData.PageType, IsPageInteractive);
            if (!isPageInteractive)
            {
                // In the non-interactive case, we don't modify the route data.
                return routeData;
            }

            // In the interactive case, we wrap the route data with a new one that
            // renders the original page interactively.
            var test = routeData;
            var test2 = interactiveRenderMode;
            return new(typeof(InteractivePage), new Dictionary<string, object?>
            {
                [nameof(InteractivePage.InteractiveRenderMode)] = interactiveRenderMode,
                [nameof(InteractivePage.RouteData)] = routeData,
            });
        }

        private static bool IsPageInteractive(Type pageType)
            => pageType.GetCustomAttribute<InteractivePageAttribute>() is not null;

        private sealed class InteractivePage : IComponent
        {
            private RenderHandle _renderHandle;

            [Parameter]
            public IComponentRenderMode InteractiveRenderMode { get; set; } = default!;

            [Parameter]
            public Microsoft.AspNetCore.Components.RouteData RouteData { get; set; } = default!;

            public void Attach(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                parameters.SetParameterProperties(this);
                _renderHandle.Render(Render);
                return Task.CompletedTask;
            }

            private void Render(RenderTreeBuilder builder)
            {
                builder.OpenComponent(0, RouteData.PageType);

                foreach (var kvp in RouteData.RouteValues)
                {
                    builder.AddComponentParameter(1, kvp.Key, kvp.Value);
                }

                builder.AddComponentRenderMode(InteractiveRenderMode);
                builder.CloseComponent();
            }
        }
    }
}
