using System;
using System.Windows.Controls;
using Microsoft.Phone.Controls.Primitives;

namespace Heatpump_Control
{
    // A non-looping numbers data source for the LoopingController
    // Possible to set minimum, maximum and default values

    public class NumbersDataSource : ILoopingSelectorDataSource
    {
        private int minimumValue = 10;
        private int maximumValue = 100;
        private int defaultValue = 10;
        private int selectedItem = 1;

        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        protected virtual void OnSelectedChanged(SelectionChangedEventArgs e)
        {
            var selectionChanged = SelectionChanged;
            if (selectionChanged != null)
                selectionChanged(this, e);
        }

        public object GetNext(object relativeTo)
        {
            var nextValue = ((int)relativeTo) + 1;

            if (nextValue > Maximum)
                return null;
            else
                return nextValue;
        }

        public object GetPrevious(object relativeTo)
        {
            var previousValue = ((int)relativeTo) - 1;

            if (previousValue < Minimum)
                return null;
            else
                return previousValue;
        }

        public object SelectedItem
        {
            get
            {
                return selectedItem;
            }

            set
            {
                var oldValue = selectedItem;
                var newValue = (int)value;

                if (oldValue == newValue)
                    return;

                selectedItem = newValue;
                OnSelectedChanged(new SelectionChangedEventArgs(new[] { oldValue }, new[] { newValue }));
            }
        }

        public int Minimum
        {
            get
            {
                return minimumValue;
            }

            set
            {
                minimumValue = value;
                if (selectedItem < minimumValue)
                    SelectedItem = value;
            }
        }

        public int Maximum
        {
            get
            {
                return maximumValue;
            }

            set
            {
                maximumValue = value;
                if (selectedItem > maximumValue)
                    SelectedItem = value;
            }
        }

        public int Default
        {
            get
            {
                return defaultValue;
            }

            set
            {
                defaultValue = value;
                if (defaultValue > maximumValue)
                    defaultValue = maximumValue;
                if (defaultValue < minimumValue)
                    defaultValue = minimumValue;
                SelectedItem = defaultValue;
            }
        }

        public bool IsSelectedValue
        {
            get
            {
                return true;
            }

            set
            {
            }
        }
    }
}