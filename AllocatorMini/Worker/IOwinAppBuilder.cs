using Owin;

namespace Worker
{
    public interface IOwinAppBuilder
    {
        void Configuration(IAppBuilder app, IWorker worker);
    }
}
