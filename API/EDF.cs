using System.Collections.Generic;
using System.Xml.Serialization;
using GTA.Math;

namespace MapEditor.API
{
    

    // XML DEFINITIONS

    [XmlRoot("edf")]
    public class EDFDefinition
    {
        [XmlElement("element")]
        public List<EDFElement> Elements = new List<EDFElement>();
    }

    public class EDFElement
    {
        [XmlAttribute("name")]
        public string Name;

        [XmlAttribute("friendlyname")]
        public string FriendlyName;

        [XmlAttribute("instructions")]
        public string Instructions;

        [XmlElement("data")] public List<EDFData> Data = new List<EDFData>();

        // Representation
        [XmlAttribute("object")] public List<EDFRepresentationObject> Objects = new List<EDFRepresentationObject>();
        [XmlAttribute("vehicle")] public List<EDFRepresentationObject> Vehicles = new List<EDFRepresentationObject>();
        [XmlAttribute("ped")] public List<EDFRepresentationObject> Peds = new List<EDFRepresentationObject>();
        [XmlAttribute("marker")] public List<EDFRepresentationObject> Markers = new List<EDFRepresentationObject>();
    }

    public enum EDFDatatype
    {
        posx,
        posy,
        posz,
        rotx,
        roty,
        rotz,
        modelhash,
        modelname,
        dynamic,
        weapon,
        id,
    }

    public class EDFData
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("type")]
        public EDFDatatype Type;
        [XmlAttribute("default")]
        public string Default;
    }

    public class EDFRepresentationObject
    {
        [XmlAttribute("posX")]
        public float PosX;
        [XmlAttribute("posY")]
        public float PosY;
        [XmlAttribute("posZ")]
        public float PosZ;

        [XmlAttribute("rotX")]
        public float RotX;
        [XmlAttribute("rotY")]
        public float RotY;
        [XmlAttribute("rotZ")]
        public float RotZ;

        [XmlAttribute("model")]
        public int Model;
    }

    public class EDFRepresentationVehicle
    {
        [XmlAttribute("posX")]
        public float PosX;
        [XmlAttribute("posY")]
        public float PosY;
        [XmlAttribute("posZ")]
        public float PosZ;


        [XmlAttribute("model")]
        public int Model;
    }

    public class EDFRepresentationPed
    {
        [XmlAttribute("posX")]
        public float PosX;
        [XmlAttribute("posY")]
        public float PosY;
        [XmlAttribute("posZ")]
        public float PosZ;


        [XmlAttribute("model")]
        public int Model;
    }

    public class EDFRepresentationMarker
    {
        public EDFRepresentationMarker()
        {
            Color = "#ffffffff";
            Size = 1f;
        }

        [XmlAttribute("posX")]
        public float PosX;
        [XmlAttribute("posY")]
        public float PosY;
        [XmlAttribute("posZ")]
        public float PosZ;


        [XmlAttribute("type")]
        public int Type;

        [XmlAttribute("color")]
        public string Color;

        [XmlAttribute("size")]
        public float Size;
    }
}