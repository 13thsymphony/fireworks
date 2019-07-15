using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HDRFireworks
{
    // Adds gravity and basic ballistics (e.g. drag).
    class ProjectileParams
    {
        public static float _velocityXScale = 0.05f;
        public static float _velocityYScale = 1.0f;

        public ProjectileParams(Random rng)
        {
            VelocityMs = new Point(
                (rng.NextDouble() - 0.5) * ProjectileParams._velocityXScale,
                 rng.NextDouble()        * ProjectileParams._velocityYScale);

            MassKg = 0.1f;
            DragCoeff = 0.5f; // Sphere
            FrontAreaM2 = 0.03f; // ~10cm diameter tube
        }

        public Point VelocityMs { get; set; }
        public float MassKg { get; set; }
        public float DragCoeff { get; set; }
        /// <summary>
        /// Frontal area in meters^2.
        /// </summary>
        public float FrontAreaM2 { get; set; }
    }

    class Projectile : Particle
    {
        protected static float _Grav = -9.8f; // Earth gravitational acceleration -9.8m/s^2 downwards
        protected static float _Air = 1.2f; // Air density 1.2kg/m^3

        protected ProjectileParams _projParams;

        // New for Projectile.
        public virtual void Initialize(double initTimeMs,
                                       Vector2 initPosMeters,
                                       Random rng,
                                       ParticleParams pm,
                                       ProjectileParams projParam,
                                       float metersPerDip = 1.0f)
        {
            InitializeProjectile(initTimeMs, initPosMeters, rng, pm, projParam, metersPerDip);
        }

        // Hides Particle base class.
        protected new void InitializeParticle(double initTimeMs,
                                  Vector2 initPosMeters,
                                  Random rng,
                                  ParticleParams pm,
                                  float metersPerDip)
        {
            var projParam = new ProjectileParams(rng);
            InitializeProjectile(initTimeMs, initPosMeters, rng, pm, projParam, metersPerDip);
        }

        // Specialization for Projectile class.
        protected virtual void InitializeProjectile(double initTimeMs,
                                          Vector2 initPosMeters,
                                          Random rng,
                                          ParticleParams pm,
                                          ProjectileParams projParam,
                                          float metersPerDip)
        {
            _projParams = projParam;
        }

        public override void Update(double timeMs)
        {
            var vel2_x = Math.Pow(_projParams.VelocityMs.X, 2);
            var vel2_y = Math.Pow(_projParams.VelocityMs.Y, 2);
            var delta = (_currTimeMs - _lastTimeMs);

            // DragForce = DragCoeff * 0.5 * FluidDensity * FlowVelocity^2 * FrontalArea
            var d_f_x = _projParams.DragCoeff * 0.5 * _Air * vel2_x * _projParams.FrontAreaM2;
            var d_f_y = _projParams.DragCoeff * 0.5 * _Air * vel2_y * _projParams.FrontAreaM2;

            // Drag_DeltaVelocity = DragForce / Mass * DeltaTime
            var drag_dv_x = d_f_x / _projParams.MassKg * delta / 1000.0;
            var drag_dv_y = d_f_y / _projParams.MassKg * delta / 1000.0;

            // Grav_DeltaVelocity = GravConst * DeltaTime
            var grav_dv = _Grav * delta / 1000.0;

            //Point newVel = new Point(_projParams.VelocityMs.X - drag_dv_x,
            //                         _projParams.VelocityMs.Y - drag_dv_y - grav_dv);

            Point newVel = new Point(_projParams.VelocityMs.X,
                                     _projParams.VelocityMs.Y - grav_dv);

            _projParams.VelocityMs = newVel;

            Vector2 newPos = new Vector2((float)(_posMeters.X + newVel.X * delta / 1000.0f),
                                         (float)(_posMeters.Y + newVel.Y * delta / 1000.0f));
        }

    }
}
