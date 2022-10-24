using StrikeTipWidget.Strike;

namespace StrikeTipWidget;

public class Broker
{
    public delegate Task OnEvent(WebhookEvent hookEvent);

    public event OnEvent Handlehook = (e) => Task.CompletedTask;

    public Task FireEvent(WebhookEvent ev)
    {
        return Handlehook(ev);
    }
}