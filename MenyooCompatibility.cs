using System.Collections.Generic;
using System.Xml.Serialization;
using GTA;

namespace MapEditor
{
    public class MenyooCompatibility
    {
        [XmlRoot(ElementName = "ReferenceCoords")]
        public class ReferenceCoords
        {
            [XmlElement(ElementName = "X")]
            public float X { get; set; }
            [XmlElement(ElementName = "Y")]
            public float Y { get; set; }
            [XmlElement(ElementName = "Z")]
            public float Z { get; set; }
        }

        [XmlRoot(ElementName = "PositionRotation")]
        public class PositionRotation
        {
            [XmlElement(ElementName = "X")]
            public float X { get; set; }
            [XmlElement(ElementName = "Y")]
            public float Y { get; set; }
            [XmlElement(ElementName = "Z")]
            public float Z { get; set; }
            [XmlElement(ElementName = "Pitch")]
            public float Pitch { get; set; }
            [XmlElement(ElementName = "Roll")]
            public float Roll { get; set; }
            [XmlElement(ElementName = "Yaw")]
            public float Yaw { get; set; }
        }

        [XmlRoot(ElementName = "Attachment")]
        public class Attachment
        {
            [XmlAttribute(AttributeName = "isAttached")]
            public string IsAttached { get; set; }
        }

        [XmlRoot(ElementName = "Placement")]
        public class Placement
        {
            [XmlElement(ElementName = "ModelHash")]
            public string ModelHash { get; set; }
            [XmlElement(ElementName = "Type")]
            public int Type { get; set; }
            [XmlElement(ElementName = "Dynamic")]
            public bool Dynamic { get; set; }
            [XmlElement(ElementName = "HashName")]
            public string HashName { get; set; }
            [XmlElement(ElementName = "InitialHandle")]
            public int InitialHandle { get; set; }
            [XmlElement(ElementName = "OpacityLevel")]
            public int OpacityLevel { get; set; }
            [XmlElement(ElementName = "LodDistance")]
            public int LodDistance { get; set; }
            [XmlElement(ElementName = "IsVisible")]
            public bool IsVisible { get; set; }
            [XmlElement(ElementName = "Health")]
            public int Health { get; set; }
            [XmlElement(ElementName = "IsOnFire")]
            public bool IsOnFire { get; set; }
            [XmlElement(ElementName = "IsInvincible")]
            public bool IsInvincible { get; set; }
            [XmlElement(ElementName = "IsBulletProof")]
            public bool IsBulletProof { get; set; }
            [XmlElement(ElementName = "IsCollisionProof")]
            public bool IsCollisionProof { get; set; }
            [XmlElement(ElementName = "IsExplosionProof")]
            public bool IsExplosionProof { get; set; }
            [XmlElement(ElementName = "IsFireProof")]
            public bool IsFireProof { get; set; }
            [XmlElement(ElementName = "IsMeleeProof")]
            public bool IsMeleeProof { get; set; }
            [XmlElement(ElementName = "IsOnlyDamagedByPlayer")]
            public bool IsOnlyDamagedByPlayer { get; set; }
            [XmlElement(ElementName = "PositionRotation")]
            public PositionRotation PositionRotation { get; set; }
            [XmlElement(ElementName = "Attachment")]
            public Attachment Attachment { get; set; }
            [XmlElement(ElementName = "PedProperties")]
            public PedProperties PedProperties { get; set; }
            [XmlElement(ElementName = "VehicleProperties")]
            public VehicleProperties VehicleProperties { get; set; }

            public Placement()
            {
                IsOnlyDamagedByPlayer = false;
                IsMeleeProof = false;
                IsFireProof = false;
                IsExplosionProof = false;
                IsCollisionProof = false;
                IsBulletProof = false;
                IsInvincible = false;
                IsOnFire = false;
                Health = 200;
                IsVisible = true;
                LodDistance = 2900;
                OpacityLevel = 0xff;
                HashName = null;

            }
        }

        [XmlRoot(ElementName = "PedProps")]
        public class PedProps
        {
            [XmlElement(ElementName = "_0")]
            public string _0 { get; set; }
            [XmlElement(ElementName = "_1")]
            public string _1 { get; set; }
            [XmlElement(ElementName = "_2")]
            public string _2 { get; set; }
            [XmlElement(ElementName = "_3")]
            public string _3 { get; set; }
            [XmlElement(ElementName = "_4")]
            public string _4 { get; set; }
            [XmlElement(ElementName = "_5")]
            public string _5 { get; set; }
            [XmlElement(ElementName = "_6")]
            public string _6 { get; set; }
            [XmlElement(ElementName = "_7")]
            public string _7 { get; set; }
            [XmlElement(ElementName = "_8")]
            public string _8 { get; set; }
            [XmlElement(ElementName = "_9")]
            public string _9 { get; set; }
        }

        [XmlRoot(ElementName = "PedComps")]
        public class PedComps
        {
            [XmlElement(ElementName = "_0")]
            public string _0 { get; set; }
            [XmlElement(ElementName = "_1")]
            public string _1 { get; set; }
            [XmlElement(ElementName = "_2")]
            public string _2 { get; set; }
            [XmlElement(ElementName = "_3")]
            public string _3 { get; set; }
            [XmlElement(ElementName = "_4")]
            public string _4 { get; set; }
            [XmlElement(ElementName = "_5")]
            public string _5 { get; set; }
            [XmlElement(ElementName = "_6")]
            public string _6 { get; set; }
            [XmlElement(ElementName = "_7")]
            public string _7 { get; set; }
            [XmlElement(ElementName = "_8")]
            public string _8 { get; set; }
            [XmlElement(ElementName = "_9")]
            public string _9 { get; set; }
            [XmlElement(ElementName = "_10")]
            public string _10 { get; set; }
            [XmlElement(ElementName = "_11")]
            public string _11 { get; set; }
        }

        [XmlRoot(ElementName = "PedProperties")]
        public class PedProperties
        {
            [XmlElement(ElementName = "IsStill")]
            public string IsStill { get; set; }
            [XmlElement(ElementName = "CurrentWeapon")]
            public string CurrentWeapon { get; set; }
            [XmlElement(ElementName = "PedProps")]
            public PedProps PedProps { get; set; }
            [XmlElement(ElementName = "PedComps")]
            public PedComps PedComps { get; set; }
            [XmlElement(ElementName = "RelationshipGroupAltered")]
            public string RelationshipGroupAltered { get; set; }
            [XmlElement(ElementName = "RelationshipGroup")]
            public string RelationshipGroup { get; set; }
            [XmlElement(ElementName = "ScenarioActive")]
            public string ScenarioActive { get; set; }
            [XmlElement(ElementName = "AnimActive")]
            public string AnimActive { get; set; }
        }

        [XmlRoot(ElementName = "Colours")]
        public class Colours
        {
            [XmlElement(ElementName = "Primary")]
            public string Primary { get; set; }
            [XmlElement(ElementName = "Secondary")]
            public string Secondary { get; set; }
            [XmlElement(ElementName = "Pearl")]
            public string Pearl { get; set; }
            [XmlElement(ElementName = "Rim")]
            public string Rim { get; set; }
            [XmlElement(ElementName = "Mod1_a")]
            public string Mod1_a { get; set; }
            [XmlElement(ElementName = "Mod1_b")]
            public string Mod1_b { get; set; }
            [XmlElement(ElementName = "Mod1_c")]
            public string Mod1_c { get; set; }
            [XmlElement(ElementName = "Mod2_a")]
            public string Mod2_a { get; set; }
            [XmlElement(ElementName = "Mod2_b")]
            public string Mod2_b { get; set; }
            [XmlElement(ElementName = "IsPrimaryColourCustom")]
            public string IsPrimaryColourCustom { get; set; }
            [XmlElement(ElementName = "IsSecondaryColourCustom")]
            public string IsSecondaryColourCustom { get; set; }
            [XmlElement(ElementName = "tyreSmoke_R")]
            public string TyreSmoke_R { get; set; }
            [XmlElement(ElementName = "tyreSmoke_G")]
            public string TyreSmoke_G { get; set; }
            [XmlElement(ElementName = "tyreSmoke_B")]
            public string TyreSmoke_B { get; set; }
            [XmlElement(ElementName = "LrInterior")]
            public string LrInterior { get; set; }
            [XmlElement(ElementName = "LrDashboard")]
            public string LrDashboard { get; set; }
        }

        [XmlRoot(ElementName = "Neons")]
        public class Neons
        {
            [XmlElement(ElementName = "Left")]
            public string Left { get; set; }
            [XmlElement(ElementName = "Right")]
            public string Right { get; set; }
            [XmlElement(ElementName = "Front")]
            public string Front { get; set; }
            [XmlElement(ElementName = "Back")]
            public string Back { get; set; }
            [XmlElement(ElementName = "R")]
            public string R { get; set; }
            [XmlElement(ElementName = "G")]
            public string G { get; set; }
            [XmlElement(ElementName = "B")]
            public string B { get; set; }
        }

        [XmlRoot(ElementName = "DoorsOpen")]
        public class DoorsOpen
        {
            [XmlElement(ElementName = "BackLeftDoor")]
            public string BackLeftDoor { get; set; }
            [XmlElement(ElementName = "BackRightDoor")]
            public string BackRightDoor { get; set; }
            [XmlElement(ElementName = "FrontLeftDoor")]
            public string FrontLeftDoor { get; set; }
            [XmlElement(ElementName = "FrontRightDoor")]
            public string FrontRightDoor { get; set; }
            [XmlElement(ElementName = "Hood")]
            public string Hood { get; set; }
            [XmlElement(ElementName = "Trunk")]
            public string Trunk { get; set; }
            [XmlElement(ElementName = "Trunk2")]
            public string Trunk2 { get; set; }
        }

        [XmlRoot(ElementName = "DoorsBroken")]
        public class DoorsBroken
        {
            [XmlElement(ElementName = "BackLeftDoor")]
            public string BackLeftDoor { get; set; }
            [XmlElement(ElementName = "BackRightDoor")]
            public string BackRightDoor { get; set; }
            [XmlElement(ElementName = "FrontLeftDoor")]
            public string FrontLeftDoor { get; set; }
            [XmlElement(ElementName = "FrontRightDoor")]
            public string FrontRightDoor { get; set; }
            [XmlElement(ElementName = "Hood")]
            public string Hood { get; set; }
            [XmlElement(ElementName = "Trunk")]
            public string Trunk { get; set; }
            [XmlElement(ElementName = "Trunk2")]
            public string Trunk2 { get; set; }
        }

        [XmlRoot(ElementName = "TyresBursted")]
        public class TyresBursted
        {
            [XmlElement(ElementName = "FrontLeft")]
            public string FrontLeft { get; set; }
            [XmlElement(ElementName = "FrontRight")]
            public string FrontRight { get; set; }
            [XmlElement(ElementName = "_2")]
            public string _2 { get; set; }
            [XmlElement(ElementName = "_3")]
            public string _3 { get; set; }
            [XmlElement(ElementName = "BackLeft")]
            public string BackLeft { get; set; }
            [XmlElement(ElementName = "BackRight")]
            public string BackRight { get; set; }
            [XmlElement(ElementName = "_6")]
            public string _6 { get; set; }
            [XmlElement(ElementName = "_7")]
            public string _7 { get; set; }
            [XmlElement(ElementName = "_8")]
            public string _8 { get; set; }
        }

        [XmlRoot(ElementName = "Mods")]
        public class Mods
        {
            [XmlElement(ElementName = "_0")]
            public string _0 { get; set; }
            [XmlElement(ElementName = "_1")]
            public string _1 { get; set; }
            [XmlElement(ElementName = "_2")]
            public string _2 { get; set; }
            [XmlElement(ElementName = "_3")]
            public string _3 { get; set; }
            [XmlElement(ElementName = "_4")]
            public string _4 { get; set; }
            [XmlElement(ElementName = "_5")]
            public string _5 { get; set; }
            [XmlElement(ElementName = "_6")]
            public string _6 { get; set; }
            [XmlElement(ElementName = "_7")]
            public string _7 { get; set; }
            [XmlElement(ElementName = "_8")]
            public string _8 { get; set; }
            [XmlElement(ElementName = "_9")]
            public string _9 { get; set; }
            [XmlElement(ElementName = "_10")]
            public string _10 { get; set; }
            [XmlElement(ElementName = "_11")]
            public string _11 { get; set; }
            [XmlElement(ElementName = "_12")]
            public string _12 { get; set; }
            [XmlElement(ElementName = "_13")]
            public string _13 { get; set; }
            [XmlElement(ElementName = "_14")]
            public string _14 { get; set; }
            [XmlElement(ElementName = "_15")]
            public string _15 { get; set; }
            [XmlElement(ElementName = "_16")]
            public string _16 { get; set; }
            [XmlElement(ElementName = "_17")]
            public string _17 { get; set; }
            [XmlElement(ElementName = "_18")]
            public string _18 { get; set; }
            [XmlElement(ElementName = "_19")]
            public string _19 { get; set; }
            [XmlElement(ElementName = "_20")]
            public string _20 { get; set; }
            [XmlElement(ElementName = "_21")]
            public string _21 { get; set; }
            [XmlElement(ElementName = "_22")]
            public string _22 { get; set; }
            [XmlElement(ElementName = "_23")]
            public string _23 { get; set; }
            [XmlElement(ElementName = "_24")]
            public string _24 { get; set; }
            [XmlElement(ElementName = "_25")]
            public string _25 { get; set; }
            [XmlElement(ElementName = "_26")]
            public string _26 { get; set; }
            [XmlElement(ElementName = "_27")]
            public string _27 { get; set; }
            [XmlElement(ElementName = "_28")]
            public string _28 { get; set; }
            [XmlElement(ElementName = "_29")]
            public string _29 { get; set; }
            [XmlElement(ElementName = "_30")]
            public string _30 { get; set; }
            [XmlElement(ElementName = "_31")]
            public string _31 { get; set; }
            [XmlElement(ElementName = "_32")]
            public string _32 { get; set; }
            [XmlElement(ElementName = "_33")]
            public string _33 { get; set; }
            [XmlElement(ElementName = "_34")]
            public string _34 { get; set; }
            [XmlElement(ElementName = "_35")]
            public string _35 { get; set; }
            [XmlElement(ElementName = "_36")]
            public string _36 { get; set; }
            [XmlElement(ElementName = "_37")]
            public string _37 { get; set; }
            [XmlElement(ElementName = "_38")]
            public string _38 { get; set; }
            [XmlElement(ElementName = "_39")]
            public string _39 { get; set; }
            [XmlElement(ElementName = "_40")]
            public string _40 { get; set; }
            [XmlElement(ElementName = "_41")]
            public string _41 { get; set; }
            [XmlElement(ElementName = "_42")]
            public string _42 { get; set; }
            [XmlElement(ElementName = "_43")]
            public string _43 { get; set; }
            [XmlElement(ElementName = "_44")]
            public string _44 { get; set; }
            [XmlElement(ElementName = "_45")]
            public string _45 { get; set; }
            [XmlElement(ElementName = "_46")]
            public string _46 { get; set; }
            [XmlElement(ElementName = "_47")]
            public string _47 { get; set; }
            [XmlElement(ElementName = "_48")]
            public string _48 { get; set; }
        }

        [XmlRoot(ElementName = "VehicleProperties")]
        public class VehicleProperties
        {
            [XmlElement(ElementName = "Colours")]
            public Colours Colours { get; set; }
            [XmlElement(ElementName = "Livery")]
            public string Livery { get; set; }
            [XmlElement(ElementName = "NumberPlateText")]
            public string NumberPlateText { get; set; }
            [XmlElement(ElementName = "NumberPlateIndex")]
            public string NumberPlateIndex { get; set; }
            [XmlElement(ElementName = "WheelType")]
            public string WheelType { get; set; }
            [XmlElement(ElementName = "WheelsInvisible")]
            public string WheelsInvisible { get; set; }
            [XmlElement(ElementName = "WindowTint")]
            public string WindowTint { get; set; }
            [XmlElement(ElementName = "BulletProofTyres")]
            public string BulletProofTyres { get; set; }
            [XmlElement(ElementName = "DirtLevel")]
            public string DirtLevel { get; set; }
            [XmlElement(ElementName = "PaintFade")]
            public string PaintFade { get; set; }
            [XmlElement(ElementName = "RoofState")]
            public string RoofState { get; set; }
            [XmlElement(ElementName = "EngineOn")]
            public string EngineOn { get; set; }
            [XmlElement(ElementName = "EngineHealth")]
            public string EngineHealth { get; set; }
            [XmlElement(ElementName = "Neons")]
            public Neons Neons { get; set; }
            [XmlElement(ElementName = "DoorsOpen")]
            public DoorsOpen DoorsOpen { get; set; }
            [XmlElement(ElementName = "DoorsBroken")]
            public DoorsBroken DoorsBroken { get; set; }
            [XmlElement(ElementName = "TyresBursted")]
            public TyresBursted TyresBursted { get; set; }
            [XmlElement(ElementName = "ModExtras")]
            public string ModExtras { get; set; }
            [XmlElement(ElementName = "Mods")]
            public Mods Mods { get; set; }
        }

        [XmlRoot(ElementName = "SpoonerPlacements")]
        public class SpoonerPlacements
        {
            [XmlElement(ElementName = "ClearDatabase")]
            public bool ClearDatabase { get; set; }
            [XmlElement(ElementName = "ClearWorld")]
            public bool ClearWorld { get; set; }
            [XmlElement(ElementName = "IPLsToLoad")]
            public string IPLsToLoad { get; set; }
            [XmlElement(ElementName = "IPLsToRemove")]
            public string IPLsToRemove { get; set; }
            [XmlElement(ElementName = "ReferenceCoords")]
            public ReferenceCoords ReferenceCoords { get; set; }
            [XmlElement(ElementName = "Placement")]
            public List<Placement> Placement { get; set; }

            public SpoonerPlacements()
            {
                Placement = new List<Placement>();
                var gamePos = Game.Player.Character.Position;
                ReferenceCoords = new ReferenceCoords()
                {
                    X = gamePos.X,
                    Y = gamePos.Y,
                    Z = gamePos.Z,
                };


            }
        }
    }
}