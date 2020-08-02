using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public sealed class SchComponent : SchGraphicalObject, IComponent, IEnumerable<SchPrimitive>
    {
        public override int Record => 1;
        public string LibReference { get; set; }
        public string ComponentDescription { get; set; }
        public string UniqueId { get; internal set; }
        public int CurrentPartId { get; set; }
        public int PartCount { get; internal set; }
        public int DisplayModeCount { get; set; }
        public int DisplayMode { get; set; }
        public bool ShowHiddenPins { get; set; }
        public string LibraryPath { get; set; }
        public string SourceLibraryName { get; set; }
        public string SheetPartFileName { get; set; }
        public string TargetFileName { get; set; }
        public bool OverrideColors { get; set; }
        public bool DesignatorLocked { get; set; }
        public bool PartIdLocked { get; set; }
        public string DesignItemId { get; set; }
        public int ComponentKind { get; set; }
        public string AliasList { get; set; }   
        public int AllPinCount => Primitives.OfType<SchPin>().Count();

        public SchDesignator Designator { get; private set; }
        public SchParameter Comment { get; private set; }
        public SchImplementationList Implementations { get; private set; }

        string IComponent.Name => LibReference;
        string IComponent.Description => ComponentDescription;

        public SchComponent()
        {
            DisplayModeCount = 1;
            OwnerPartId = -1;
            PartCount = 1;
            CurrentPartId = 1;
            LibraryPath = "*";
            SourceLibraryName = "*";
            SheetPartFileName = "*";
            TargetFileName = "*";
            UniqueId = Utils.GenerateUniqueId();
            AreaColor = ColorTranslator.FromWin32(11599871);
            Color = ColorTranslator.FromWin32(128);
            PartIdLocked = true;

            Designator = new SchDesignator
            {
                Name = "Designator",
                Location = new CoordPoint(-5, 5)
            };
            Comment = new SchParameter
            {
                Name = "Comment",
                Location = new CoordPoint(-5, -15)
            };
            Implementations = new SchImplementationList();
        }

        IEnumerator<SchPrimitive> IEnumerable<SchPrimitive>.GetEnumerator() => Primitives.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Primitives.GetEnumerator();

        public override CoordRect CalculateBounds() =>
            CoordRect.Union(GetPrimitivesOfType<Primitive>(false)
                .Select(p => p.CalculateBounds()));

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            LibReference = p["LIBREFERENCE"].AsStringOrDefault();
            ComponentDescription = p["COMPONENTDESCRIPTION"].AsStringOrDefault();
            UniqueId = p["UNIQUEID"].AsStringOrDefault();
            CurrentPartId = p["CURRENTPARTID"].AsIntOrDefault();
            PartCount = p["PARTCOUNT"].AsIntOrDefault() - 1; // for some reason this number is one more than the actual number of parts
            DisplayModeCount = p["DISPLAYMODECOUNT"].AsIntOrDefault();
            DisplayMode = p["DISPLAYMODE"].AsIntOrDefault();
            ShowHiddenPins = p["SHOWHIDDENPINS"].AsBool();
            LibraryPath = p["LIBRARYPATH"].AsStringOrDefault("*");
            SourceLibraryName = p["SOURCELIBRARYNAME"].AsStringOrDefault("*");
            SheetPartFileName = p["SHEETPARTFILENAME"].AsStringOrDefault("*");
            TargetFileName = p["TARGETFILENAME"].AsStringOrDefault("*");
            OverrideColors = p["OVERIDECOLORS"].AsBool();
            DesignatorLocked = p["DESIGNATORLOCKED"].AsBool();
            PartIdLocked = p["PARTIDLOCKED"].AsBool();
            DesignItemId = p["DESIGNITEMID"].AsStringOrDefault();
            ComponentKind = p["COMPONENTKIND"].AsIntOrDefault();
            AliasList = p["ALIASLIST"].AsStringOrDefault();
            Console.WriteLine($"{LibReference} \t {DesignItemId} \t {LibReference != DesignItemId}");
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.SetBookmark();
            p.Add("LIBREFERENCE", LibReference);
            p.Add("COMPONENTDESCRIPTION", ComponentDescription);
            p.Add("PARTCOUNT", PartCount + 1);
            p.Add("DISPLAYMODECOUNT", DisplayModeCount);
            p.MoveKeys("INDEXINSHEET");
            p.Add("CURRENTPARTID", CurrentPartId);
            p.Add("SHOWHIDDENPINS", ShowHiddenPins);
            p.Add("LIBRARYPATH", LibraryPath);
            p.Add("SOURCELIBRARYNAME", SourceLibraryName);
            p.Add("SHEETPARTFILENAME", SheetPartFileName);
            p.Add("TARGETFILENAME", TargetFileName);
            p.Add("UNIQUEID", UniqueId);
            p.MoveKey("AREACOLOR");
            p.MoveKey("COLOR");
            p.Add("DISPLAYMODE", DisplayMode);
            p.Add("OVERIDECOLORS", OverrideColors);
            p.Add("DESIGNATORLOCKED", DesignatorLocked);
            p.Add("PARTIDLOCKED", PartIdLocked, false);
            p.Add("ALIASLIST", AliasList);
            p.Add("DESIGNITEMID", DesignItemId);
            p.Add("COMPONENTKIND", ComponentKind);
            p.Add("ALLPINCOUNT", AllPinCount);
        }

        protected override bool DoAdd(SchPrimitive primitive)
        {
            if (primitive == null) return false;

            if (primitive is SchPin pin)
            {
                pin.Designator = pin.Designator ??
                    Utils.GenerateDesignator(GetPrimitivesOfType<SchPin>(false).Select(p => p.Designator));
                pin.Name = pin.Name ?? pin.Designator;
            }
            else if (primitive is SchParameter parameter)
            {
                if (parameter.Name == "Designator")
                {
                    Designator = (SchDesignator)parameter;
                    return false;
                }
                else if (parameter.Name == "Comment")
                {
                    Comment = parameter;
                    return false;
                }
            }
            else if (primitive is SchImplementationList implementationList)
            {
                Implementations = implementationList;
                return false;
            }
            
            if (primitive.OwnerPartId < 1 || primitive.OwnerPartId >= PartCount)
            {
                primitive.OwnerPartId = CurrentPartId;
            }

            return true;
        }

        protected override IEnumerable<SchPrimitive> DoGetParameters()
        {
            return new SchPrimitive[] { Designator, Comment, Implementations };
        }

        public void AddPart()
        {
            CurrentPartId = ++PartCount;
        }

        public IEnumerable<SchPrimitive> RemovePart(int partId)
        {
            if (partId < 1 || partId > PartCount) return Enumerable.Empty<SchPrimitive>();

            // remove the part primitives
            var partPrimitives = Primitives.Where(p => p.OwnerPartId == partId);
            foreach (var p in partPrimitives) Remove(p);

            // decrease part id for primitives belonging to parts of higher value than the one removed
            foreach (var p in Primitives.Where(p => p.OwnerPartId > partId))
            {
                p.OwnerPartId--;
            }

            return partPrimitives;
        }
    }
}
