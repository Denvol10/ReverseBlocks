using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using System.Collections.ObjectModel;
using ReverseBlocks.Models;

namespace ReverseBlocks
{
    public class RevitModelForfard
    {
        private UIApplication Uiapp { get; set; } = null;
        private Application App { get; set; } = null;
        private UIDocument Uidoc { get; set; } = null;
        private Document Doc { get; set; } = null;

        public RevitModelForfard(UIApplication uiapp)
        {
            Uiapp = uiapp;
            App = uiapp.Application;
            Uidoc = uiapp.ActiveUIDocument;
            Doc = uiapp.ActiveUIDocument.Document;
        }

        #region Блоки пролетного строения
        public List<Element> BlockElements { get; set; }

        private string _blockElementIds;
        public string BlockElementIds
        {
            get => _blockElementIds;
            set => _blockElementIds = value;
        }

        public void GetBlockElementsBySelection()
        {
            BlockElements = RevitGeometryUtils.GetElementsBySelection(Uiapp, out _blockElementIds);
        }

        // Проверка на то существуют ли блоки в модели
        public bool IsBlocksExistInModel(string elemIdsInSettings)
        {
            var elemIds = RevitGeometryUtils.GetIdsByString(elemIdsInSettings);

            return RevitGeometryUtils.IsElemsExistInModel(Doc, elemIds, typeof(FamilyInstance));
        }
        #endregion

        // Получение блоков из Settings
        public void GetBlocksBySettings(string elemIdsInSettings)
        {
            var elemIds = RevitGeometryUtils.GetIdsByString(elemIdsInSettings);
            BlockElements = RevitGeometryUtils.GetBlocksById(Doc, elemIds);
        }

        #region Изменить ориентацию блоков
        public void ChangeBlocksOrientation(bool isReversed, bool isMirrored, bool isTurned)
        {
            var blockSetup = new List<(Element Element, IList<ElementId> Points, double Length)>();

            foreach (var block in BlockElements)
            {
                Parameter lengthParameter = block.LookupParameter("Длина блока");
                double length = lengthParameter.AsDouble();

                var adaptivePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(block as FamilyInstance);

                blockSetup.Add((block, adaptivePointIds, length));
            }


            using (Transaction trans = new Transaction(Doc, "Changed Bloks Orientation"))
            {
                trans.Start();
                foreach (var block in blockSetup)
                {
                    var firstReferencePoint = Doc.GetElement(block.Points.First()) as ReferencePoint;
                    var secondReferencePoint = Doc.GetElement(block.Points.ElementAt(1)) as ReferencePoint;
                    var thirdReferencePoint = Doc.GetElement(block.Points.Last()) as ReferencePoint;

                    XYZ alongVector = (secondReferencePoint.Position - firstReferencePoint.Position).Normalize() * block.Length;
                    XYZ normalVector = (thirdReferencePoint.Position - firstReferencePoint.Position).Normalize();

                    XYZ newFirstPointPosition = firstReferencePoint.Position;
                    XYZ newSecondPointPosition = secondReferencePoint.Position;
                    XYZ newThirdPointPosition = thirdReferencePoint.Position;

                    Parameter blockTurnParameter = block.Element.get_Parameter(BuiltInParameter.FLEXIBLE_INSTANCE_FLIP);
                    int turnOposite = blockTurnParameter.AsInteger() == 0 ? 1 : 0;

                    if (isReversed)
                    {
                        newFirstPointPosition = newFirstPointPosition + alongVector;
                        newSecondPointPosition = newSecondPointPosition
                                                     + alongVector.Normalize()
                                                     * (block.Length - UnitUtils.ConvertToInternalUnits(2, UnitTypeId.Meters));

                        newThirdPointPosition = newThirdPointPosition + alongVector;

                        blockTurnParameter.Set(turnOposite);
                    }

                    if (isMirrored)
                    {
                        newThirdPointPosition = newThirdPointPosition + normalVector.Negate() * UnitUtils.ConvertToInternalUnits(2, UnitTypeId.Meters);
                    }

                    if (isTurned)
                    {
                        blockTurnParameter.Set(turnOposite);
                    }

                    firstReferencePoint.Position = newFirstPointPosition;
                    secondReferencePoint.Position = newSecondPointPosition;
                    thirdReferencePoint.Position = newThirdPointPosition;
                }
                trans.Commit();

                Element firstElement = BlockElements.FirstOrDefault();
                if(!(firstElement is null))
                {
                    Uidoc.ShowElements(firstElement);
                    Uidoc.RefreshActiveView();
                }
            }
        }
        #endregion
    }
}
