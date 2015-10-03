﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using ShapefileTools;
using System.Data;

using Matrix = SharpDX.Matrix;
using Vector3 = SharpDX.Vector3;

namespace TerraViewer
{
    public class Reticle
    {

        public static Dictionary<int, Reticle> Reticles = new Dictionary<int, Reticle>();
        public int Id = 0;
        public Color Color = Color.Red;
        double alt;

        public double Alt
        {
            get { return alt; }
            set
            {
                alt = value;
                if (alt > 90)
                {
                    alt = 90;
                }
                if (alt < 0)
                {
                    alt = 0;
                }
            }
        }

        double az;

        public double Az
        {
            get { return az; }
            set
            {
                az = (value + 360) % 360;
            }
        }

        public static void Set(int id, double alt, double az, Color color)
        {
            Reticle reticle = null;

            if (Reticles.ContainsKey(id))
            {
                reticle = Reticles[id];
            }
            else
            {
                reticle = new Reticle();
                Reticles.Add(id, reticle);
            }

            reticle.Id = id;
            reticle.Alt = alt;
            reticle.Az = az;
            reticle.Color = color;
            reticle.Visible.TargetState = true;

        }

        public static void Hide(int id, bool instant)
        {
            if (Reticles.ContainsKey(id))
            {
                if (instant)
                {
                    Reticles[id].Visible.State = false;
                }
                else
                {
                    Reticles[id].Visible.TargetState = false;
                }
            }
        }

        public static void Show(int id, bool instant)
        {
            if (Reticles.ContainsKey(id))
            {
                if (instant)
                {
                    Reticles[id].Visible.State = true;
                }
                else
                {
                    Reticles[id].Visible.TargetState = true;
                }
            }
        }

        public BlendState Visible = new BlendState(false, 1000, 0);

        static Texture11 reticleImage;

        static readonly PositionColoredTextured[] points = new PositionColoredTextured[4];

    
        private void ComputePoints()
        {

            var center = Coordinates.GeoTo3dDouble(alt, az + 90);
            var up = Coordinates.GeoTo3dDouble(alt + 90, az + 90);

            var width = .03;
            var height = width;

            var left = Vector3d.Cross(center, up);
            var right = Vector3d.Cross(up, center);

            left.Normalize();
            right.Normalize();
            up.Normalize();

            var upTan = Vector3d.Cross(center, right);

            upTan.Normalize();

            left.Multiply(width);
            right.Multiply(width);
            var top = upTan;
            var bottom = -upTan;
            top.Multiply(height);
            bottom.Multiply(height);
            var ul = center;
            ul.Add(top);
            ul.Add(left);
            var ur = center;
            ur.Add(top);
            ur.Add(right);

            var ll = center;
            ll.Add(left);
            ll.Add(bottom);

            var lr = center;
            lr.Add(right);
            lr.Add(bottom);

            points[0].Position = ul.Vector4;
            points[1].Position = ll.Vector4;
            points[3].Position = lr.Vector4;
            points[2].Position = ur.Vector4;

            points[0].Color = Color;
            points[1].Color = Color;
            points[3].Color = Color;
            points[2].Color = Color;

            points[0].Tu = 0;
            points[0].Tv = 0;
            points[1].Tu = 0;
            points[1].Tv = 1;
            points[3].Tu = 1;
            points[3].Tv = 1;
            points[2].Tu = 1;
            points[2].Tv = 0;
        }

        internal static void DrawAll(RenderContext11 rendercontext)
        {
            foreach (var reticle in Reticles.Values)
            {
                if (reticle.Visible.State)
                {
                    reticle.Draw(rendercontext);
                }
            }
        }

        internal void Draw(RenderContext11 renderContext)
        {
            if (reticleImage == null)
            {
                reticleImage = Texture11.FromBitmap(Properties.Resources.Reticle);
            }

            ComputePoints();

            Sprite2d.Draw(renderContext, points, 4, reticleImage, SharpDX.Direct3D.PrimitiveTopology.TriangleStrip);

     
        }
    }

}
