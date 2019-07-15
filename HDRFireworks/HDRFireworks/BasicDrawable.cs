using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using System;
using System.Numerics;
using Windows.Foundation;

namespace HDRFireworks
{
    class BasicDrawableParams
    {
        static float _baseLuminanceScale = 20.0f;
        static double _decayTimeScaleMs = 3000.0f;

        public BasicDrawableParams(Random rng)
        {
            BaseHue = new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
            BaseLuminance = (float)rng.NextDouble() * _baseLuminanceScale;
            DecayTimeMs = rng.NextDouble() * _decayTimeScaleMs;
            MaxLifetimeMs = 10000.0;
            CanDisposeLuminance = 0.05f;
        }

        /// <summary>
        /// Hue in scRGB values. Is scaled (in an implementation-specific manner) by BaseLuminance.
        /// </summary>
        public Vector3 BaseHue { get; set; }
        
        /// <summary>
        /// Setpoint for luminance in scRGB values (1.0 = 80 nits). In BasicDrawable, corresponds to peak luminance.
        /// </summary>
        public float BaseLuminance { get; set; }
        /// <summary>
        /// Maximum allowed liftime before this become destroyable, in MS.
        /// </summary>
        public double MaxLifetimeMs { get; set; }
        /// <summary>
        /// In scRGB values. When the luminance falls below this level, becomes destroyable.
        /// </summary>
        public float CanDisposeLuminance { get; set; }
        /// <summary>
        /// Decay time constant in milliseconds. In BasicDrawable, corresponds to time to reach 1/e peak luminance.
        /// </summary>
        public double DecayTimeMs { get; set; }
    }

    /// <summary>
    /// A minimal implementation of IDrawable, other implementations can use this base class for convenience.
    /// HDR aware (uses scRGB colors).
    /// </summary>
    class BasicDrawable : IDrawable
    {
        static float _maxRenderRadiusDips = 1.0f;

        // Should be invariant for lifetime of object.
        protected Random _rng;
        protected double _initTimeMs;

        // Can change.
        protected bool _isInitialized;
        protected Vector2 _posMeters;
        protected float _metersPerDip;
        protected float _requestedRenderRadiusDips;
        protected double _lastTimeMs;
        protected double _currTimeMs;
        protected bool _canDispose;

        protected BasicDrawableParams _basicDrawableParams;

        public void Initialize(double initTimeMs,
                               Vector2 initPosMeters,
                               Random rng,
                               float metersPerDip = 1.0f)
        {
            BasicDrawableParams pm = new BasicDrawableParams(rng);

            InitializeInternal(initTimeMs, initPosMeters, rng, pm, metersPerDip);
        }

        public void Initialize(double initTimeMs,
                               Vector2 initPosMeters,
                               Random rng,
                               BasicDrawableParams pm,
                               float metersPerDip = 1.0f)
        {
            InitializeInternal(initTimeMs, initPosMeters, rng, pm, metersPerDip);
        }

        protected void InitializeInternal(double initTimeMs,
                                          Vector2 initPosMeters,
                                          Random rng,
                                          BasicDrawableParams pm,
                                          float metersPerDip)
        {
            _posMeters = initPosMeters;
            _initTimeMs = _lastTimeMs = _currTimeMs = initTimeMs;
            _rng = rng;
            _metersPerDip = metersPerDip;

            _canDispose = false;
            _requestedRenderRadiusDips = 1.0f;
            _basicDrawableParams = pm;

            _isInitialized = true;
        }

        public void Update(double timeMs)
        {
            if (_isInitialized == false)
            {
                throw new InvalidOperationException();
            }

            if (timeMs < _currTimeMs)
            {
                throw new ArgumentOutOfRangeException();
            }

            _lastTimeMs = _currTimeMs;
            _currTimeMs = timeMs;
            var delta = _currTimeMs - _lastTimeMs;

            _basicDrawableParams.BaseLuminance *= (float)Math.Exp(-delta / _basicDrawableParams.DecayTimeMs);

            if (_basicDrawableParams.BaseLuminance <= _basicDrawableParams.CanDisposeLuminance ||
                _currTimeMs - _initTimeMs > _basicDrawableParams.MaxLifetimeMs)
            {
                _canDispose = true;
            }
        }

        public void Render(CanvasDrawingSession ds, Vector4 boundsDips)
        {
            if (_isInitialized == false)
            {
                throw new InvalidOperationException();
            }

            Vector4 color = new Vector4(_basicDrawableParams.BaseHue * _basicDrawableParams.BaseLuminance, 1.0f);
            CanvasSolidColorBrush brush = CanvasSolidColorBrush.CreateHdr(ds, color);

            var posDips = new Vector2(_posMeters.X / _metersPerDip, _posMeters.Y / _metersPerDip);

            // TODO: respect boundsDips
            ds.FillCircle(posDips, _maxRenderRadiusDips, brush);
        }


        public Vector2 PosMeters { get { return _posMeters; } set { _posMeters = PosMeters; } }
        public Vector2 PosDips { get { return new Vector2(_posMeters.X / _metersPerDip, _posMeters.Y / _metersPerDip); } }
        public float MetersPerDip { get { return _metersPerDip; } set { _metersPerDip = MetersPerDip; } }
        public double CurrTimeMs { get { return _currTimeMs; } set { _currTimeMs = CurrTimeMs; } }
        public bool CanDispose { get { return _canDispose; } }
        public float RequestedRenderRadiusDips { get { return _requestedRenderRadiusDips; } }
    }
}
