namespace BladeMode.Slicing
{
    /// <summary>
    /// Implement on any component attached to a SliceableSurface prop to receive a
    /// callback immediately before the GameObject is destroyed during slicing.
    /// Use this to clear targeting references, deregister from events, stop audio sources, etc.
    /// The two new hull GameObjects are already spawned when this fires, so you can transfer
    /// state to them if needed.
    /// </summary>
    public interface ISliceEventListener
    {
        void OnBeforeSliced();
    }
}
