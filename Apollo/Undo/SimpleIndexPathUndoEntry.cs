using Apollo.Selection;

namespace Apollo.Undo {
    public abstract class SimpleIndexPathUndoEntry<T, I>: SimplePathUndoEntry<T, I> where T: ISelect {
        int index;

        protected override void Action(T item, I element) => Action(item, index, element);
        protected virtual void Action(T item, int index, I element) {}

        public SimpleIndexPathUndoEntry(string desc, T child, int index, I undo, I redo)
        : base(desc, child, undo, redo) => this.index = index;
    }
}