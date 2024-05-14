namespace PlacementStore
{
    public interface IOwinAppBuilder
    {
        void Configuration(string publishAddress, Owin.IAppBuilder app, IPlacementStore placementStore);
    }
}
