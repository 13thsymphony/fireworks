using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using System;
using System.Numerics;
using Windows.Foundation;

namespace HDRFireworks
{
    class ParticleParams
    {
        static float _baseLuminanceScale = 20.0f;
        static double _decayTimeScale = 3.0f; // In seconds
        static float _velocityXScale = 0.05f;
        static float _velocityYScale = 1.0f;
        static float _dragCoeffSphere = 0.5f;
        static float _dragCoeffNone = 0.0f;

        public ParticleParams(Random rng)
        {
            BaseHue = new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
            BaseLuminance = (float)rng.NextDouble() * _baseLuminanceScale;
            DecayTime = rng.NextDouble() * _decayTimeScale;
            MaxLifetime = 30.0;
            CanDisposeLuminance = 0.05f;
            Velocity = new Point(
                (rng.NextDouble() - 0.5) * _velocityXScale,
                 rng.NextDouble() * _velocityYScale);
            Mass = 0.1f;
            DragCoeff = _dragCoeffSphere; // Sphere
            FrontArea = 0.03f; // ~10cm diameter tube
        }

        /// <summary>
        /// Hue in scRGB values. Gets multiplied by BaseLuminance.
        /// </summary>
        public Vector3 BaseHue { get; set; }
        
        /// <summary>
        /// Setpoint for peak luminance in scRGB values (1.0 = 80 nits).
        /// </summary>
        public float BaseLuminance { get; set; }
        /// <summary>
        /// Maximum allowed liftime before this become destroyable, in seconds.
        /// </summary>
        public double MaxLifetime { get; set; }
        /// <summary>
        /// In scRGB values. When the luminance falls below this level, becomes destroyable.
        /// </summary>
        public float CanDisposeLuminance { get; set; }
        /// <summary>
        /// Decay time constant in seconds; corresponds to time to reach 1/e peak luminance.
        /// </summary>
        public double DecayTime { get; set; }
        /// <summary>
        /// In meters/second.
        /// </summary>
        public Point Velocity { get; set; }
        /// <summary>
        /// In kilograms.
        /// </summary>
        public float Mass { get; set; }
        public float DragCoeff { get; set; }
        /// <summary>
        /// Frontal area in meters^2.
        /// </summary>
        public float FrontArea { get; set; }
    }

    /// <summary>
    /// Point particle with basic physics (drag, gravity).
    /// </summary>
    class Particle : IDrawable
    {
        protected static float _maxRenderRadiusDips = 1.0f;
        protected static float _Grav = -9.8f; // Earth gravitational acceleration -9.8m/s^2 downwards
        protected static float _Air = 1.2f; // Air density 1.2kg/m^3

        // Should be invariant for lifetime of object.
        protected Random _rng;
        protected double _initTime; // In seconds.

        // Can change.
        protected bool _isInitialized;
        protected Vector2 _position; // In meters.
        protected float _metersPerDip;
        protected float _requestedRenderRadiusDips;
        protected double _lastTime; // In seconds.
        protected double _currTime; // In seconds.
        protected bool _canDispose;

        protected ParticleParams _params;

        public void Initialize(double initTimeMs,
                               Vector2 initPosMeters,
                               Random rng,
                               float metersPerDip = 1.0f)
        {
            ParticleParams pm = new ParticleParams(rng);

            InitializeParticle(initTimeMs, initPosMeters, rng, pm, metersPerDip);
        }

        public void Initialize(double initTimeMs,
                               Vector2 initPosMeters,
                               Random rng,
                               ParticleParams pm,
                               float metersPerDip = 1.0f)
        {
            InitializeParticle(initTimeMs, initPosMeters, rng, pm, metersPerDip);
        }

        protected virtual void InitializeParticle(double initTimeMs,
                                          Vector2 initPosMeters,
                                          Random rng,
                                          ParticleParams pm,
                                          float metersPerDip)
        {
            _position = initPosMeters;
            _initTime = _lastTime = _currTime = initTimeMs / 1000.0f;
            _rng = rng;
            _metersPerDip = metersPerDip;

            _canDispose = false;
            _requestedRenderRadiusDips = 1.0f;
            _params = pm;

            _isInitialized = true;
        }

        public virtual void Update(double timeMs)
        {
            if (_isInitialized == false)
            {
                throw new InvalidOperationException();
            }

            if (timeMs < _currTime)
            {
                throw new ArgumentOutOfRangeException();
            }

            _lastTime = _currTime;
            _currTime = timeMs / 1000.0f;

            UpdateColor();
            UpdatePosition();
        }

        protected virtual void UpdateColor()
        {
            var delta = _currTime - _lastTime;

            _params.BaseLuminance *= (float)Math.Exp(-delta / _params.DecayTime);

            if (_params.BaseLuminance <= _params.CanDisposeLuminance ||
                _currTime - _initTime > _params.MaxLifetime)
            {
                _canDispose = true;
            }
        }

        protected virtual void UpdatePosition()
        {
            var vel2_x = Math.Pow(_params.Velocity.X, 2);
            var vel2_y = Math.Pow(_params.Velocity.Y, 2);
            var delta = (_currTime - _lastTime);

            // DragForce = DragCoeff * 0.5 * FluidDensity * FlowVelocity^2 * FrontalArea
            var d_f_x = _params.DragCoeff * 0.5 * _Air * vel2_x * _params.FrontArea;
            var d_f_y = _params.DragCoeff * 0.5 * _Air * vel2_y * _params.FrontArea;

            // Drag_DeltaVelocity = DragForce / Mass * DeltaTime
            var drag_dv_x = d_f_x / _params.Mass * delta;
            var drag_dv_y = d_f_y / _params.Mass * delta;

            // Grav_DeltaVelocity = GravConst * DeltaTime
            var grav_dv = _Grav * delta;

            //Point newVel = new Point(_params.Velocity.X - drag_dv_x,
            //                         _params.Velocity.Y - drag_dv_y - grav_dv);

            Point newVel = new Point(_params.Velocity.X,
                                     _params.Velocity.Y - grav_dv);

            _params.Velocity = newVel;

            _position = new Vector2((float)(_position.X + newVel.X * delta),
                                    (float)(_position.Y + newVel.Y * delta));

        }

        public virtual void Render(CanvasDrawingSession ds, Vector4 boundsDips)
        {
            if (_isInitialized == false)
            {
                throw new InvalidOperationException();
            }

            Vector4 color = new Vector4(_params.BaseHue * _params.BaseLuminance, 1.0f);
            CanvasSolidColorBrush brush = CanvasSolidColorBrush.CreateHdr(ds, color);

            var posDips = new Vector2(_position.X / _metersPerDip, _position.Y / _metersPerDip);

            // TODO: respect boundsDips
            ds.FillCircle(posDips, _maxRenderRadiusDips, brush);
        }


        public Vector2 PosMeters { get { return _position; } set { _position = PosMeters; } }
        public Vector2 PosDips { get { return new Vector2(_position.X / _metersPerDip, _position.Y / _metersPerDip); } }
        public float MetersPerDip { get { return _metersPerDip; } set { _metersPerDip = MetersPerDip; } }
        public double CurrTimeSec { get { return _currTime; } set { _currTime = CurrTimeSec; } }
        public bool CanDispose { get { return _canDispose; } }
        public float RequestedRenderRadiusDips { get { return _requestedRenderRadiusDips; } }
    }
}
