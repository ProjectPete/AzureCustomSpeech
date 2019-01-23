using DigitalEyes.VoiceToText.Desktop.Models;
using DigitalEyes.VoiceToText.Desktop.ViewModels;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DigitalEyes.VoiceToText.Desktop.Views
{
    public class ListViewMaxWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return FrameworkElement.MaxWidthProperty;
            }

            return (-190 + (double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
    
    public class NullVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool nullStateWanted = false;
            if (parameter != null)
            {
                nullStateWanted = (bool)parameter;
            }
            
            if (nullStateWanted)
            {
                return value == null ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return value != null ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class LeftMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
            {
                return -10D;
            }

            if (values[0] == null)
            {

            }

            double startMills = (double)values[0]; // ((TextPart)values[0]).StartMills;

            double scale = (double)values[1];

            //return new TranslateTransform { X = startMills * scale };

            //return new Thickness((startMills * scale), -1, -1, -1);
            double calc =  (startMills  * scale);
            return calc;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DebugConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class WidthScaleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double width = (double)values[0];
                double scale = (double)values[1];
                return width * scale;
            }
            catch (Exception exc)
            {
                Debug.WriteLine($"Error binding: {exc}");
                return 40;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PassSelfToDataContextHackConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var vm = values[0] as TrackSnippetViewModel;
            var rectangle = values[1] as Rectangle;
            vm.PositionBar = rectangle;

            return "hello"; // Sory for the hax
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProjectMenuItemConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var projects = new List<DE_VTT_Project>((IEnumerable<DE_VTT_Project>)values[0]);
            var openCommand = values[1] as RelayCommand<DE_VTT_Project>;

            var items = projects.Select(a => new MenuItem { Header = a.Name, IsChecked = true, Command = openCommand, CommandParameter = a }).ToList();

            return items;

            //var item = new MenuItem
            //{
            //    Header = project.Name,
            //    Command = openCommand
            //};
            //if (parameter != null)
            //{
            //    item.CommandParameter = parameter;
            //}

            //return item;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
