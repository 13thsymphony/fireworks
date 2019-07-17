using Windows.Foundation;

namespace HDRFireworks
{
    // Represents something that can be drawn using Win2D and simulates its visual state over time.
    interface IDrawable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rng">Accessing this must be thread safe.</param>
        void Initialize(double initTimeMs,
                        System.Numerics.Vector2 initPosMeters,
                        System.Random rng,
                        float metersPerDip = 1.0f);
        void Update(double timeMs);

        /// <remarks>
        /// TODO: How to encode the default no restrictions behavior?
        /// This method requires the implementer to understand DIPs.
        /// </remarks>
        /// <param name="ds">Relies on caller to dispose.</param>
        /// <param name="boundsDips">Optional: defaults to {0, 0, 0, 0} which means "no restrictions".</param>
        void Render(Microsoft.Graphics.Canvas.CanvasDrawingSession ds, System.Numerics.Vector4 boundsDips);

        // Most of the property setters are dangerous and only should be used with knowledge of the implementation.
        // Generally, all values dealing with spatial distances (e.g. DIPs) are in float to match Win2D.
        // All values dealing with time are in double.
        System.Numerics.Vector2 PosMeters { get; set; }
        System.Numerics.Vector2 PosDips { get; }
        float MetersPerDip { get; set; }
        double CurrTimeSec { get; set; }
        /// <summary>
        /// Can the caller delete this object as the rendered output is complete at this point in time.
        /// </summary>
        bool CanDispose { get; }
        /// <summary>
        /// The bounds that the object needs to fully render its output at this point in time.
        /// </summary>
        float RequestedRenderRadiusDips { get; }
    }
}
