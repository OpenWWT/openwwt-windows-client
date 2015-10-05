﻿using System;
using System.Text;
using System.Drawing;
using System.IO;
using System.Xml;

namespace TerraViewer
{
    public enum AltUnits { Meters, Feet, Inches, Miles, Kilometers, AstronomicalUnits, LightYears, Parsecs, MegaParsecs, Custom };
    public enum FadeType { In, Out, Both, None };
    public abstract class Layer : IAnimatable
    {
        public virtual LayerUI GetPrimaryUI()
        {
            return null;
        }

        public Guid ID = Guid.NewGuid();

        public bool LoadedFromTour = false;

        private float opacity = 1.0f;
        [LayerProperty]
        public virtual float Opacity
        {
            get
            {
                return opacity;
            }
            set
            {
                if (opacity != value)
                {
                    version++;
                    opacity = value;
                }

            }
        }

        public bool opened = false;
        public virtual bool Opened
        {
            get
            {
                return opened;
            }
            set
            {
                if (opened != value)
                {
                    version++;
                    opened = value;
                }
            }
        }

        private DateTime startTime = DateTime.MinValue;
        [LayerProperty]
        public DateTime StartTime
        {
            get { return startTime; }
            set
            {
                if (startTime != value)
                {
                    version++;
                    startTime = value;
                }
            }
        }
        private DateTime endTime = DateTime.MaxValue;

        [LayerProperty]
        public DateTime EndTime
        {
            get { return endTime; }
            set
            {
                if (endTime != value)
                {
                    version++;
                    endTime = value;
                }
            }
        }

        private TimeSpan fadeSpan = TimeSpan.Zero;

        [LayerProperty]
        public TimeSpan FadeSpan
        {
            get { return fadeSpan; }
            set
            {
                if (fadeSpan != value)
                {
                    version++;
                    fadeSpan = value;
                }
            }
        }

        private FadeType fadeType = FadeType.None;

        [LayerProperty]
        public FadeType FadeType
        {
            get { return fadeType; }
            set {
                if (fadeType != value)
                {
                    Version++;
                    fadeType = value;
                }
            }
        }
        protected int version = 0;
    
        [LayerProperty]
        public int Version
        {
            get { return version; }
            set { version = value; }
        }

        public virtual IPlace FindClosest(Coordinates target, float distance, IPlace closestPlace, bool astronomical)
        {
            return closestPlace;
        }

        public virtual bool HoverCheckScreenSpace(Point cursor)
        {
            return false;
        }

        public virtual bool ClickCheckScreenSpace(Point cursor)
        {
            return false;
        }


        public virtual bool Draw(RenderContext11 renderContext, float opacity, bool flat)
        {

            return true;
        }

        public virtual bool PreDraw(RenderContext11 renderContext, float opacity)
        {
            return true;
        }

        public virtual bool UpadteData(object data, bool purgeOld, bool purgeAll, bool hasHeader)
        {

            return true;
        }

        public virtual bool CanCopyToClipboard()
        {
            return false;
        }

        public virtual void CopyToClipboard()
        {
            return;
        }

        public virtual double[] GetParams()
        {
            var paramList = new double[5];
            paramList[0] = color.R/255;
            paramList[1] = color.G/255;
            paramList[2] = color.B/255;
            paramList[3] = color.A/255;
            paramList[4] = opacity;


            return paramList;
        }

        public virtual string[] GetParamNames()
        {
            return new[] { "Color.Red", "Color.Green", "Color.Blue", "Color.Alpha", "Opacity" };
        }

        public virtual BaseTweenType[] GetParamTypes()
        {
            return new[] { BaseTweenType.Linear, BaseTweenType.Linear, BaseTweenType.Linear, BaseTweenType.Linear, BaseTweenType.Linear };
        }

        public virtual void SetParams(double[] paramList)
        {
            if (paramList.Length == 5)
            {
                opacity = (float)paramList[4];
                color = Color.FromArgb((int)(paramList[3] * 255), (int)(paramList[0] * 255), (int)(paramList[1]*255), (int)(paramList[2]*255));
            }
        }


        public string GetIndentifier()
        {
            return ID.ToString();
        }

        public string GetName()
        {
            return Name;
        }


        public virtual IUiController GetEditUI()
        {
            return null;
        }


        public virtual void CleanUp()
        {
        }

        private string name;
        [LayerProperty]
        public virtual string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    version++;
                    name = value;
                }
            }
        }

        public override string ToString()
        {
            return name;
        }

        private string referenceFrame;

        public string ReferenceFrame
        {
            get { return referenceFrame; }
            set { referenceFrame = value; }
        }

        public bool SetProp(string name, string value)
        {
            var thisType = GetType();
            var pi = thisType.GetProperty(name);
            var safeToSet = false;
            var layerPropType = typeof(LayerProperty);
            var attributes = pi.GetCustomAttributes(false);
            foreach (var var in attributes)
            {
                if (var.GetType() == layerPropType)
                {
                    safeToSet = true;
                    break;
                }
            }

            if (safeToSet)
            {
                //Convert.ChangeType(
                if (pi.PropertyType.BaseType == typeof(Enum))
                {
                    pi.SetValue(this, Enum.Parse(pi.PropertyType, value), null);
                }
                else if (pi.PropertyType == typeof(TimeSpan))
                {
                    pi.SetValue(this, TimeSpan.Parse(value), null);
                }
                else if (pi.PropertyType == typeof(Vector3d))
                {
                    pi.SetValue(this, Vector3d.Parse(value), null);
                }   
                else
                {
                    pi.SetValue(this, Convert.ChangeType(value, pi.PropertyType), null);
                }
            }


            return safeToSet;
        }

        public bool SetProps(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);


            XmlNode root = doc["LayerApi"];

            XmlNode LayerNode = root["Layer"];
            foreach (XmlAttribute attrib in LayerNode.Attributes)
            {
                if (attrib.Name == "Class")
                {
                    continue;
                }
                if (!SetProp(attrib.Name, attrib.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public string GetProp(string name)
        {
            var thisType = GetType();
            var pi = thisType.GetProperty(name);
            var safeToGet = false;
            var layerPropType = typeof(LayerProperty);
            var attributes = pi.GetCustomAttributes(false);
            foreach (var var in attributes)
            {
                if (var.GetType() == layerPropType)
                {
                    safeToGet = true;
                    break;
                }
            }

            if (safeToGet)
            {
                return pi.GetValue(this, null).ToString();
            }


            return null;
        }

        public string GetProps()
        {
            var ms = new MemoryStream();
            using (var xmlWriter = new XmlTextWriter(ms, Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
                xmlWriter.WriteStartElement("LayerApi");
                xmlWriter.WriteElementString("Status", "Success");
                xmlWriter.WriteStartElement("Layer");
                xmlWriter.WriteAttributeString("Class", GetType().ToString().Replace("TerraViewer.",""));


                var thisType = GetType();
                var properties = thisType.GetProperties();

                var layerPropType = typeof(LayerProperty);

                foreach (var pi in properties)
                {
                    var safeToGet = false;

                    var attributes = pi.GetCustomAttributes(false);
                    foreach (var var in attributes)
                    {
                        if (var.GetType() == layerPropType)
                        {
                            safeToGet = true;
                            break;
                        }
                    }

                    if (safeToGet)
                    {
                        xmlWriter.WriteAttributeString(pi.Name, pi.GetValue(this, null).ToString());
                    }

                }
                xmlWriter.WriteEndElement();
                xmlWriter.WriteFullEndElement();
                xmlWriter.Close();

            }
            var data = ms.GetBuffer();
            return Encoding.UTF8.GetString(data);

        }

        private Color color = Color.White;

        public virtual Color Color
        {
            get { return color; }
            set
            {
                if (color.ToArgb() != value.ToArgb())
                {
                    color = value;
                    version++;
                    ColorChanged();
                    //todo should this invalidate and cleanup
                }
 
            }
        }

        public virtual void ColorChanged()
        {
            CleanUp();
        }

        [LayerProperty]
        public virtual string ColorValue
        {
            get
            {
                var saveCol = new SavedColor(color.ToArgb());
                return saveCol.Save();
            }
            set
            {
                var newVal = SavedColor.Load(value);

                if (color != newVal)
                {
                    version++;
                    Color = newVal;
                    //todo should this invalidate and cleanup
                }
 
            }
        }

        private bool enabled = true;

        [LayerProperty]
        public virtual bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled != value)
                {
                    version++;
                    enabled = value;
                }
                //todo notify of change
            }
        }

        protected bool astronomical = false;

        [LayerProperty]
        public bool Astronomical
        {
            get { return astronomical; }
            set
            {
                if (astronomical != value)
                {
                    version++;
                    astronomical = value;
                }
            }
        }


        public virtual void SaveToXml(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("Layer");
            xmlWriter.WriteAttributeString("Id", ID.ToString());
            xmlWriter.WriteAttributeString("Type", GetType().FullName);
            xmlWriter.WriteAttributeString("Name", Name);
            xmlWriter.WriteAttributeString("ReferenceFrame", referenceFrame);
            xmlWriter.WriteAttributeString("Color", SavedColor.Save(color));
            xmlWriter.WriteAttributeString("Opacity", opacity.ToString());
            xmlWriter.WriteAttributeString("StartTime", StartTime.ToString());
            xmlWriter.WriteAttributeString("EndTime", EndTime.ToString());
            xmlWriter.WriteAttributeString("FadeSpan", FadeSpan.ToString());
            xmlWriter.WriteAttributeString("FadeType", FadeType.ToString());

            WriteLayerProperties(xmlWriter);

            xmlWriter.WriteEndElement();
        }
        public virtual void InitializeFromXml(XmlNode node)
        {

        }    
        
        internal static Layer FromXml(XmlNode layerNode, bool someFlag)
        {
            var layerClassName = layerNode.Attributes["Type"].Value;

            var overLayType = Type.GetType(layerClassName);

            var newLayer = (Layer)Activator.CreateInstance(overLayType);
            newLayer.FromXml(layerNode);
            return newLayer;
        }

        private void FromXml(XmlNode node)
        {
            ID = new Guid(node.Attributes["Id"].Value);
            Name = node.Attributes["Name"].Value;
            referenceFrame = node.Attributes["ReferenceFrame"].Value;
            color = SavedColor.Load(node.Attributes["Color"].Value);
            opacity = Convert.ToSingle(node.Attributes["Opacity"].Value);

            if (node.Attributes["StartTime"] != null)
            {
                StartTime = Convert.ToDateTime(node.Attributes["StartTime"].Value);
            }

            if (node.Attributes["EndTime"] != null)
            {
                EndTime = Convert.ToDateTime(node.Attributes["EndTime"].Value);
            }

            if (node.Attributes["FadeSpan"] != null)
            {
                FadeSpan = TimeSpan.Parse(node.Attributes["FadeSpan"].Value);
            }

            if (node.Attributes["FadeType"] != null)
            {
                FadeType = (FadeType)Enum.Parse(typeof(FadeType), node.Attributes["FadeType"].Value);
            }

            InitializeFromXml(node);
        }



        public virtual void LoadData(string path)
        {
            return;
        }

        public virtual void AddFilesToCabinet(FileCabinet fc)
        {
            return;
        }

        public virtual void WriteLayerProperties(XmlTextWriter xmlWriter)
        {
            return;
        }


/* End Load Save Support
         * 
         * 
         * 
         * 
         * 
         * 
         */
        




    }
    class LayerCollection : Layer
    {
        public override bool Draw(RenderContext11 renderContext, float opacity, bool flat)
        {
            return base.Draw(renderContext, opacity, false);
        }

        
     
    }
    
  
    public class DomainValue
    {
        public DomainValue()
        {
        }

        public DomainValue(string text, int markerIndex)
        {
            Text = text;
            MarkerIndex = markerIndex;
        }

        public string Text;
        public int MarkerIndex = 4;
        public Bitmap CustomMarker = null;

    }
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class LayerProperty : Attribute
    {
    }
}
