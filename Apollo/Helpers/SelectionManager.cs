using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Input;

using Apollo.Core;
using Apollo.Interfaces;

namespace Apollo.Helpers {
    public class SelectionManager {
        public ISelect Start { get; private set; } = null;
        public ISelect End { get; private set; } = null;

        public List<ISelect> Selection {
            get {
                if (Start != null) {
                    if (End != null) {
                        ISelect left = (Start.IParentIndex.Value < End.IParentIndex.Value)? Start : End;
                        ISelect right = (Start.IParentIndex.Value < End.IParentIndex.Value)? End : Start;

                        return left.IParent.IChildren.Skip(left.IParentIndex.Value).Take(right.IParentIndex.Value - left.IParentIndex.Value + 1).ToList();
                    }
                    
                    return new List<ISelect>() {Start};
                }

                return new List<ISelect>();
            }
        }

        public void Select(ISelect select, bool shift = false) {
            if (Start != null)
                if (End != null)
                    foreach (ISelect selected in Selection)
                        selected.IInfo?.Deselect();
                else Start.IInfo?.Deselect();

            if (shift && Start != null && Start.IParent == select.IParent && Start != select)
                End = select;

            else {
                Start = select;
                End = null;
            }

            if (Start != null)
                if (End != null)
                    foreach (ISelect selected in Selection)
                        selected.IInfo?.Select();
                else Start.IInfo?.Select();
        }

        public void SelectAll() {
            ISelectParent target = Start.IParent;
            Select(target.IChildren.First());
            Select(target.IChildren.Last(), true);
        }

        public bool Move(bool right, bool shift = false) {
            ISelect target = (shift? (End?? Start) : Start);
            if (target == null) return false;

            try {
                if (right) {
                    target = target.IParent.IChildren[target.IParentIndex.Value + 1];

                } else {
                    if (target.IParentIndex.Value == 0 && target.IParent is ISelect && !target.IParent.IRoot) target = (ISelect)target.IParent;
                    else target = target.IParent.IChildren[target.IParentIndex.Value - 1];
                }

            } catch (ArgumentOutOfRangeException) {
                return false;
            }

            Select(target, shift);
            return true;
        }

        public void Expand() {
            if (Start.IParent.IViewer.IExpanded != Start.IParentIndex)
                Start.IParent.IViewer.Expand(Start.IParentIndex);
        }

        public void MoveChild() {
            Expand();

            if (Start is ISelectParent && ((ISelectParent)Start).IChildren.Count > 0)
                Select(((ISelectParent)Start).IChildren[0]);
        }

        public void Action(string action) {
            if (Start == null) return;

            ISelectParent parent = Start.IParent;
            
            int left = Start.IParentIndex.Value;
            int right = (End == null)? left: End.IParentIndex.Value;
            
            if (left > right) {
                int temp = left;
                left = right;
                right = temp;
            }

            Action(action, parent, left, right);
        }

        public void Action(string action, ISelectParent parent, int index) => Action(action, parent, index, index);

        public void Action(string action, ISelectParent parent, int left, int right) {
            if (action == "Cut") parent.IViewer?.Copy(left, right, true);
            else if (action == "Copy") parent.IViewer?.Copy(left, right);
            else if (action == "Duplicate") parent.IViewer?.Duplicate(left, right);
            else if (action == "Paste") parent.IViewer?.Paste(right);
            else if (action == "Delete") parent.IViewer?.Delete(left, right);
            else if (action == "Group") parent.IViewer?.Group(left, right);
            else if (action == "Ungroup") parent.IViewer?.Ungroup(left);
            else if (action == "Mute" || action == "Unmute") parent.IViewer?.Mute(left, right);
            else if (action == "Rename") parent.IViewer?.Rename(left, right);
            else if (action == "Export") parent.IViewer?.Export(left, right);
            else if (action == "Import") parent.IViewer?.Import(right);
        }

        public bool HandleKey(KeyEventArgs e) {
            if (Start == null) return true;

            if (e.Modifiers == Program.ControlKey) {
                if (e.Key == Key.X) Action("Cut");
                else if (e.Key == Key.C) Action("Copy");
                else if (e.Key == Key.D) Action("Duplicate");
                else if (e.Key == Key.V) Action("Paste");
                else if (e.Key == Key.G) Action("Group");
                else if (e.Key == Key.U) Action("Ungroup");
                else if (e.Key == Key.R) Action("Rename");
                else if (e.Key == Key.E) Action("Export");
                else if (e.Key == Key.I) Action("Import");
                else if (e.Key == Key.A) SelectAll();
                else return false;

            } else {
                if (e.Key == Key.Delete || e.Key == Key.Back) Action("Delete");
                else if (e.Key == Key.D0 || e.Key == Key.NumPad0) Action("Mute");
                else return false;
            }

            return true;
        }

        public void Dispose() => Start = End = null;
    }
}