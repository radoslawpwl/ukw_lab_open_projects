using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;

namespace SklepInternetowyDocker.Services;

public sealed class LocalizedPageMessagesFilter(IStringLocalizer<SharedResource> text) : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        await next();
        foreach (var entry in context.ModelState.Values)
        {
            foreach (var error in entry.Errors.ToArray())
            {
                if (string.IsNullOrEmpty(error.ErrorMessage))
                {
                    continue;
                }

                var translated = text[error.ErrorMessage];
                if (!translated.ResourceNotFound)
                {
                    var index = entry.Errors.IndexOf(error);
                    entry.Errors.RemoveAt(index);
                    entry.Errors.Insert(index, new ModelError(translated.Value));
                }
            }
        }
    }
}