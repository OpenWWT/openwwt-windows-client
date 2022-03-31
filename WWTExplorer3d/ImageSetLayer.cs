﻿using System;
using System.IO;
//using SharpDX.Toolkit.Graphics;
using System.Windows.Input;

#if WINDOWS_UWP
using XmlElement = Windows.Data.Xml.Dom.XmlElement;
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
using Point = TerraViewer.Point;
#else
using Color = System.Drawing.Color;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Point = System.Drawing.Point;
using System.Drawing;
using System.Xml;
using System.Windows.Forms;
#endif
using System.Collections.Generic;

using Vector3 = SharpDX.Vector3;


namespace TerraViewer
{
    public class ImageSetLayer : Layer, IVoLayer
    {
        IImageSet imageSet = null;

        ScaleTypes lastScale = ScaleTypes.Linear;
        double    min = 0;
        double    max = 0;

        public IImageSet ImageSet
        {
            get { return imageSet; }
            set { imageSet = value; }
        }
        string extension = "txt";
        public ImageSetLayer()
        {
           
        }

        public ImageSetLayer(IImageSet set)
        {
            imageSet = set;
#if !WINDOWS_UWP
            GetDefaultColumns();
            if (set.Projection == ProjectionType.Healpix && set.Extension == "tsv")
            {
                this.ID = GuidUtils.CreateV5(set.Name);
            }
#endif
        }

        bool overrideDefaultLayer = false;
        #if !WINDOWS_UWP
         public VoTable Table
        {
            get
            {
                return imageSet.TableMetadata;
            }
        }

        private VOTableViewer viewer = null;
        public VOTableViewer Viewer { get => viewer; set => viewer = value; }

        private TimeSeriesLayer.PlotTypes plotType = TimeSeriesLayer.PlotTypes.Gaussian;

        [LayerProperty]
        public TimeSeriesLayer.PlotTypes PlotType
        {
            get { return plotType; }
            set
            {
                if (plotType != value)
                {
                    version++;
                    plotType = value;
                }

            }
        }

        protected int latColumn = -1;

        [LayerProperty]
        public int LatColumn
        {
            get { return latColumn; }
            set
            {
                if (latColumn != value)
                {
                    version++;
                    latColumn = value;
                }
            }
        }
        protected int lngColumn = -1;

        [LayerProperty]
        public int LngColumn
        {
            get { return lngColumn; }
            set
            {
                if (lngColumn != value)
                {
                    version++;
                    lngColumn = value;
                }
            }
        }

        protected int altColumn = -1;
        [LayerProperty]
        public int AltColumn
        {
            get { return altColumn; }
            set
            {
                if (altColumn != value)
                {
                    version++;
                    altColumn = value;
                }
            }
        }

        private int markerColumn = -1;

        [LayerProperty]
        public int MarkerColumn
        {
            get { return markerColumn; }
            set
            {
                if (markerColumn != value)
                {
                    version++;
                    markerColumn = value;
                }
            }
        }

        protected int sizeColumn = -1;

        [LayerProperty]
        public int SizeColumn
        {
            get { return sizeColumn; }
            set
            {
                if (sizeColumn != value)
                {
                    version++;
                    sizeColumn = value;
                }
            }
        }

        private TimeSeriesLayer.AltTypes altType = TimeSeriesLayer.AltTypes.SeaLevel;

        [LayerProperty]
        public TimeSeriesLayer.AltTypes AltType
        {
            get { return altType; }
            set
            {
                if (altType != value)
                {
                    version++;
                    altType = value;
                }
            }
        }

        private AltUnits altUnit = AltUnits.Meters;

        [LayerProperty]
        public AltUnits AltUnit
        {
            get { return altUnit; }
            set
            {
                if (altUnit != value)
                {
                    version++;
                    altUnit = value;
                }
            }
        }

        public bool IsSiapResultSet()
        {
            return false;
        }


        public void GetDefaultColumns()
        {
            if (Table != null)
            {
                LngColumn = Table.GetRAColumn().Index;
                LatColumn = Table.GetDecColumn().Index;
            }
        }
#endif



        public bool OverrideDefaultLayer
        {
            get { return overrideDefaultLayer; }
            set { overrideDefaultLayer = value; }
        }

        public FitsImage FitsImage
        {
            get
            {
                if (imageSet.WcsImage == null || !(imageSet.WcsImage is FitsImage))
                {
                    return null;
                }
                return imageSet.WcsImage as FitsImage;
            }
        }


        public override void InitializeFromXml(XmlNode node)
        {
            XmlNode imageSetNode = node["ImageSet"];

            ImageSetHelper ish = ImageSetHelper.FromXMLNode(imageSetNode);

            if (!String.IsNullOrEmpty(ish.Url) && RenderEngine.ReplacementImageSets.ContainsKey(ish.Url))
            {
                imageSet = RenderEngine.ReplacementImageSets[ish.Url];
            }
            else
            {
                imageSet = ish;
            }

            if (node.Attributes["Extension"] != null)
            {
                extension = node.Attributes["Extension"].Value;
            }

            if (node.Attributes["ScaleType"] != null)
            {
                lastScale = (ScaleTypes) Enum.Parse(typeof(ScaleTypes), node.Attributes["ScaleType"].Value);
            }

            if (node.Attributes["MinValue"] != null)
            {
                min = double.Parse(node.Attributes["MinValue"].Value);
            }

            if (node.Attributes["MaxValue"] != null)
            {
                max = double.Parse(node.Attributes["MaxValue"].Value);
            }

            if (node.Attributes["OverrideDefault"] != null)
            {
                overrideDefaultLayer = bool.Parse(node.Attributes["OverrideDefault"].Value );
            }

        }

        // Set up lighting state to account for:
        //   - Light reflected from a nearby planet
        //   - Shadows cast by nearby planets
        public void SetupLighting(RenderContext11 renderContext)
        {
            Vector3d objPosition = new Vector3d(renderContext.World.OffsetX, renderContext.World.OffsetY, renderContext.World.OffsetZ);
            Vector3d objToLight = objPosition - renderContext.ReflectedLightPosition;
            Vector3d sunPosition = renderContext.SunPosition - renderContext.ReflectedLightPosition;
            double cosPhaseAngle = sunPosition.Length() <= 0.0 ? 1.0 : Vector3d.Dot(objToLight, sunPosition) / (objToLight.Length() * sunPosition.Length());
            float reflectedLightFactor = (float)Math.Max(0.0, cosPhaseAngle);
            reflectedLightFactor = (float)Math.Sqrt(reflectedLightFactor); // Tweak falloff of reflected light
            float hemiLightFactor = 0.0f;

            // 1. Reduce the amount of sunlight when the object is in the shadow of a planet
            // 2. Introduce some lighting due to scattering by the planet's atmosphere if it's
            //    close to the surface.
            double sunlightFactor = 1.0;
            if (renderContext.OccludingPlanetRadius > 0.0)
            {
                double objAltitude = (objPosition - renderContext.OccludingPlanetPosition).Length() - renderContext.OccludingPlanetRadius;
                hemiLightFactor = (float)Math.Max(0.0, Math.Min(1.0, 1.0 - (objAltitude / renderContext.OccludingPlanetRadius) * 300));
                reflectedLightFactor *= (1.0f - hemiLightFactor);

                // Compute the distance from the center of the object to the line between the sun and occluding planet
                // We're assuming that the radius of the object is very small relative to Earth;
                // for large objects the amount of shadow will vary, and we should use circular
                // eclipse shadows.
                Vector3d sunToPlanet = renderContext.OccludingPlanetPosition - renderContext.SunPosition;
                Vector3d objToPlanet = renderContext.OccludingPlanetPosition - objPosition;

                Vector3d hemiLightDirection = -objToPlanet;
                hemiLightDirection.Normalize();
                renderContext.HemisphereLightUp = hemiLightDirection;

                Vector3d objToSun = renderContext.SunPosition - objPosition;
                double sunPlanetDistance = sunToPlanet.Length();
                double t = -Vector3d.Dot(objToSun, sunToPlanet) / (sunPlanetDistance * sunPlanetDistance);
                if (t > 1.0)
                {
                    // Object is on the side of the planet opposite the sun, so a shadow is possible

                    // Compute the position of the object projected onto the shadow axis
                    Vector3d shadowAxisPoint = Vector3d.Add(renderContext.SunPosition, Vector3d.Multiply(sunToPlanet, t));

                    // d is the distance to the shadow axis
                    double d = (shadowAxisPoint - objPosition).Length();

                    // s is the distance from the sun along the shadow axis
                    double s = (shadowAxisPoint - renderContext.SunPosition).Length();

                    // Use the sun's radius to accurately compute the penumbra and umbra cones
                    const double solarRadius = 0.004645784; // AU
                    double penumbraRadius = renderContext.OccludingPlanetRadius + (t - 1.0) * (renderContext.OccludingPlanetRadius + solarRadius);
                    double umbraRadius = renderContext.OccludingPlanetRadius + (t - 1.0) * (renderContext.OccludingPlanetRadius - solarRadius);

                    if (d < penumbraRadius)
                    {
                        // The object is inside the penumbra, so it is at least partly shadowed
                        double minimumShadow = 0.0;
                        if (umbraRadius < 0.0)
                        {
                            // No umbra at this point; degree of shadowing is limited because the
                            // planet doesn't completely cover the sun even when the object is positioned
                            // exactly on the shadow axis.
                            double occlusion = Math.Pow(1.0 / (1.0 - umbraRadius), 2.0);
                            umbraRadius = 0.0;
                            minimumShadow = 1.0 - occlusion;
                        }

                        // Approximate the amount of shadow with linear interpolation. The accurate
                        // calculation involves computing the area of the intersection of two circles.
                        double u = Math.Max(0.0, umbraRadius);
                        sunlightFactor = Math.Max(minimumShadow, (d - u) / (penumbraRadius - u));

                        int gray = (int)(255.99f * sunlightFactor);
                        renderContext.SunlightColor = Color.FromArgb(gray, gray, gray);

                        // Reduce sky-scattered light as well
                        hemiLightFactor *= (float)sunlightFactor;
                    }
                }
            }

            renderContext.ReflectedLightColor = Color.FromArgb((int)(renderContext.ReflectedLightColor.R * reflectedLightFactor),
                                                                               (int)(renderContext.ReflectedLightColor.G * reflectedLightFactor),
                                                                               (int)(renderContext.ReflectedLightColor.B * reflectedLightFactor));
            renderContext.HemisphereLightColor = Color.FromArgb((int)(renderContext.HemisphereLightColor.R * hemiLightFactor),
                                                                               (int)(renderContext.HemisphereLightColor.G * hemiLightFactor),
                                                                               (int)(renderContext.HemisphereLightColor.B * hemiLightFactor));
        }

        public override bool Draw(RenderContext11 renderContext, float opacity, bool flat)
        {
            if (!flat)
            {
                renderContext.setRasterizerState(TriangleCullMode.CullClockwise);
            }
            renderContext.WorldBase = renderContext.World ;
            renderContext.ViewBase = renderContext.View;
            RenderEngine.Engine.MakeFrustum();
            renderContext.MakeFrustum();

            
            if (renderContext.RenderType != ImageSetType.SolarSystem)
            {
                RenderEngine.Engine.PaintLayerFullTint11(imageSet, this.Opacity * opacity * 100, Color);
            }
            else
            {
               // SetupLighting(renderContext);
                var key = new PlanetShaderKey(PlanetSurfaceStyle.Diffuse, false, 0);
                renderContext.SetupPlanetSurfaceEffect(key, 1.0f);
                renderContext.PreDraw();
                RenderEngine.Engine.DrawTiledSphere(imageSet, this.Opacity * opacity, Color);
            }

            return true;

        }
#if !WINDOWS_UWP
        protected List<Vector3> positions = new List<Vector3>();

        public override IPlace FindClosest(Coordinates target, float distance, IPlace defaultPlace, bool astronomical)
        {
            if (imageSet.TableMetadata == null)
            {
                return defaultPlace;
            }

            Vector3d searchPoint = Coordinates.GeoTo3dDouble(target.Lat, target.Lng);

            Vector3d dist;
            if (defaultPlace != null)
            {
                Vector3d testPoint = Coordinates.RADecTo3d(defaultPlace.RA, -defaultPlace.Dec, -1.0);
                dist = searchPoint - testPoint;
                distance = (float)dist.Length();
            }

            int closestItem = -1;
            int index = 0;
            int raCol = imageSet.TableMetadata.GetRAColumn().Index;
            int decCol = imageSet.TableMetadata.GetDecColumn().Index;
            foreach(VoRow row in imageSet.TableMetadata.Rows)
            {
                double Xcoord = Coordinates.ParseRA(row[raCol].ToString(), true) * 15 + 180;
                double Ycoord = Coordinates.ParseDec(row[decCol].ToString());

                Vector3d p = Coordinates.GeoTo3dDouble(Ycoord, Xcoord);

                dist = searchPoint - p;
                positions.Add(p.Vector3);

                if (dist.Length() < distance)
                {
                    distance = (float)dist.Length();
                    closestItem = index;
                }
                index++;
            }

            if (closestItem == -1)
            {
                return defaultPlace;
            }

            Coordinates pnt = Coordinates.CartesianToSpherical2(positions[closestItem]);
            int nameCol = 1; // imageSet.TableMetadata.GetColumnByUcd()
            //string name = imageSet.TableMetadata.Rows[closestItem].ColumnData[nameCol].ToString();
            string name = "";

            if (String.IsNullOrEmpty(name))
            {
                name = string.Format("RA={0}, Dec={1}", Coordinates.FormatHMS(pnt.RA), Coordinates.FormatDMS(pnt.Dec));
            }
            TourPlace place = new TourPlace(name, pnt.Lat, pnt.RA, Classification.Unidentified, "", ImageSetType.Sky, -1);

            Dictionary<String, String> rowData = new Dictionary<string, string>();
            for (int i = 0; i < imageSet.TableMetadata.Columns.Count; i++)
            {
                string colValue = imageSet.TableMetadata.Rows[closestItem][i].ToString();
                //if (i == startDateColumn || i == endDateColumn)
                //{
                //    colValue = SpreadSheetLayer.ParseDate(colValue).ToString("u");
                //}

                //if (!rowData.ContainsKey(table.Column[i].Name) && !string.IsNullOrEmpty(table.Column[i].Name))
                //{
                //    rowData.Add(table.Column[i].Name, colValue);
                //}
                //else
                {
                    rowData.Add(imageSet.TableMetadata.Column[i].Name, colValue);
                }
            }
            place.Tag = rowData;
            if (Viewer != null)
            {
                Viewer.LabelClicked(closestItem);
            }
            return place;
        }
#endif

        public override void WriteLayerProperties(XmlTextWriter xmlWriter)
        {
            if (imageSet.WcsImage != null)
            {
                xmlWriter.WriteAttributeString("Extension", Path.GetExtension(((WcsImage)imageSet.WcsImage).Filename));
            }

            if (imageSet.WcsImage is FitsImage)
            {
                FitsImage fi = imageSet.WcsImage as FitsImage;
                xmlWriter.WriteAttributeString("ScaleType", fi.lastScale.ToString());
                xmlWriter.WriteAttributeString("MinValue", fi.lastBitmapMin.ToString());
                xmlWriter.WriteAttributeString("MaxValue", fi.lastBitmapMax.ToString());
               
            }

            xmlWriter.WriteAttributeString("OverrideDefault", overrideDefaultLayer.ToString());
            
            ImageSetHelper.SaveToXml(xmlWriter, imageSet, "");
            base.WriteLayerProperties(xmlWriter);
        }

        public override void CleanUp()
        {
            base.CleanUp();
        }

        public override void AddFilesToCabinet(FileCabinet fc)
        {
            if (imageSet.WcsImage != null)
            {
                string fName = ((WcsImage)imageSet.WcsImage).Filename;

                bool copy = !fName.Contains(ID.ToString());
                string extension = Path.GetExtension(fName);

                string fileName = fc.TempDirectory + string.Format("{0}\\{1}{2}", fc.PackageID, this.ID.ToString(), extension);
                string path = fName.Substring(0, fName.LastIndexOf('\\') + 1);

                if (fName != fileName)
                {
                    if (File.Exists(fName) && !File.Exists(fileName))
                    {
                        File.Copy(fName, fileName);
                    }
                }

                if (File.Exists(fileName))
                {
                    fc.AddFile(fileName);
                }
                
            }
        }

        public override string[] GetParamNames()
        {
            return base.GetParamNames();
        }

        public override double[] GetParams()
        {
            return base.GetParams();
        }

        public override void SetParams(double[] paramList)
        {
            base.SetParams(paramList);
        }

        public override void LoadData(string path)
        {
            string filename = path.Replace(".txt",  extension);
             if (File.Exists(filename))
             {
                 imageSet.WcsImage = WcsImage.FromFile(filename);
                 if (min != 0 && max != 0)
                 {
                     FitsImage fi = imageSet.WcsImage as FitsImage;
                     fi.lastBitmapMax = max;
                     fi.lastBitmapMin = min;
                     fi.lastScale = lastScale;
                 }
             }
        }
    }

    public interface IVoLayer
    {
#if !WINDOWS_UWP
        VoTable Table { get;  }
        VOTableViewer Viewer { get; set; }
        TimeSeriesLayer.PlotTypes PlotType { get; set; }
        int LngColumn { get; set; }
        int LatColumn { get; set; }
        int AltColumn { get; set; }
        int MarkerColumn { get; set; }
        int SizeColumn { get; set; }
        AltUnits AltUnit { get; set; }
        TimeSeriesLayer.AltTypes AltType { get; set; }

        void CleanUp();
        bool IsSiapResultSet();
#endif
    }
}
