﻿//The MIT License(MIT)

//Copyright(c) 2016 Alberto Rodriguez

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveCharts.Definitions.Points;
using LiveCharts.Definitions.Series;
using LiveCharts.Dtos;
using LiveCharts.Helpers;
using LiveCharts.SeriesAlgorithms;
using LiveCharts.Wpf.Charts.Base;
using LiveCharts.Wpf.Points;

// ReSharper disable once CheckNamespace
namespace LiveCharts.Wpf
{
    /// <summary>
    /// The stacked column series compares the proportion of every series in a point
    /// </summary>
    public class StackedColumnSeries : Series.Series, IStackedColumnSeriesView
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of StackedColumnSeries class
        /// </summary>
        public StackedColumnSeries()
        {
            Model = new StackedColumnAlgorithm(this);
            InitializeDefuaults();
        }

        /// <summary>
        /// Initializes a new instance of StackedColumnSeries class, with a given mapper
        /// </summary>
        public StackedColumnSeries(object configuration)
        {
            Model = new StackedColumnAlgorithm(this);
            Configuration = configuration;
            InitializeDefuaults();
        }

        #endregion

        #region Private Properties

        #endregion

        #region Properties

        public static readonly DependencyProperty MaxColumnWidthProperty = DependencyProperty.Register(
            "MaxColumnWidth", typeof (double), typeof (StackedColumnSeries), new PropertyMetadata(default(double)));
        /// <summary>
        /// Gets or sets the maximum width of a column, any column will be capped at this value
        /// </summary>
        public double MaxColumnWidth
        {
            get { return (double) GetValue(MaxColumnWidthProperty); }
            set { SetValue(MaxColumnWidthProperty, value); }
        }

        public static readonly DependencyProperty ColumnPaddingProperty = DependencyProperty.Register(
            "ColumnPadding", typeof (double), typeof (StackedColumnSeries), new PropertyMetadata(default(double)));
        /// <summary>
        /// Gets or sets the padding between every column in this series
        /// </summary>
        public double ColumnPadding
        {
            get { return (double) GetValue(ColumnPaddingProperty); }
            set { SetValue(ColumnPaddingProperty, value); }
        }

        public static readonly DependencyProperty StackModeProperty = DependencyProperty.Register(
            "StackMode", typeof (StackMode), typeof (StackedColumnSeries), new PropertyMetadata(default(StackMode)));
        /// <summary>
        /// Gets or sets stacked mode, values or percentage
        /// </summary>
        public StackMode StackMode
        {
            get { return (StackMode) GetValue(StackModeProperty); }
            set { SetValue(StackModeProperty, value); }
        }

        #endregion

        #region Overridden Methods

        public override IChartPointView GetPointView(IChartPointView view, ChartPoint point, string label)
        {
            var pbv = (view as ColumnPointView);

            if (pbv == null)
            {
                pbv = new ColumnPointView
                {
                    IsNew = true,
                    Rectangle = new Rectangle(),
                    Data = new CoreRectangle(),
                    LabelInside = true
                };

                BindingOperations.SetBinding(pbv.Rectangle, Shape.FillProperty,
                    new Binding { Path = new PropertyPath(FillProperty), Source = this });
                BindingOperations.SetBinding(pbv.Rectangle, Shape.StrokeProperty,
                    new Binding { Path = new PropertyPath(StrokeProperty), Source = this });
                BindingOperations.SetBinding(pbv.Rectangle, Shape.StrokeThicknessProperty,
                    new Binding {Path = new PropertyPath(StrokeThicknessProperty), Source = this});
                BindingOperations.SetBinding(pbv.Rectangle, Shape.StrokeDashArrayProperty,
                    new Binding {Path = new PropertyPath(StrokeDashArrayProperty), Source = this});
                BindingOperations.SetBinding(pbv.Rectangle, Panel.ZIndexProperty,
                    new Binding {Path = new PropertyPath(Panel.ZIndexProperty), Source = this});
                BindingOperations.SetBinding(pbv.Rectangle, VisibilityProperty,
                    new Binding { Path = new PropertyPath(VisibilityProperty), Source = this });

                Model.Chart.View.AddToDrawMargin(pbv.Rectangle);
            }
            else
            {
                pbv.IsNew = false;
                point.SeriesView.Model.Chart.View
                    .EnsureElementBelongsToCurrentDrawMargin(pbv.Rectangle);
                point.SeriesView.Model.Chart.View
                    .EnsureElementBelongsToCurrentDrawMargin(pbv.HoverShape);
            }

            if (Model.Chart.RequiresHoverShape && pbv.HoverShape == null)
            {
                pbv.HoverShape = new Rectangle
                {
                    Fill = Brushes.Transparent,
                    StrokeThickness = 0
                };

                Panel.SetZIndex(pbv.HoverShape, int.MaxValue);
                BindingOperations.SetBinding(pbv.HoverShape, VisibilityProperty,
                    new Binding {Path = new PropertyPath(VisibilityProperty), Source = this});

                var wpfChart = (Chart)Model.Chart.View;
                wpfChart.AttachHoverableEventTo(pbv.HoverShape);

                Model.Chart.View.AddToDrawMargin(pbv.HoverShape);
            }

            if (DataLabels && pbv.DataLabel == null)
            {
                pbv.DataLabel = BindATextBlock(0);
                Panel.SetZIndex(pbv.DataLabel, int.MaxValue - 1);

                Model.Chart.View.AddToDrawMargin(pbv.DataLabel);
            }

            if (pbv.DataLabel != null) pbv.DataLabel.Text = label;

            return pbv;
        }

        public override void Erase()
        {
            Values.Points.ForEach(p =>
            {
                if (p.View != null)
                    p.View.RemoveFromView(Model.Chart);
            });
            Model.Chart.View.RemoveFromView(this);
        }

        #endregion

        #region Private Methods

        private void InitializeDefuaults()
        {
            SetValue(StrokeThicknessProperty, 0d);
            SetValue(MaxColumnWidthProperty, 35d);
            SetValue(ColumnPaddingProperty, 5d);
            SetValue(ForegroundProperty, Brushes.White);

            Func<ChartPoint, string> defaultLabel = x =>  Model.CurrentYAxis.GetFormatter()(x.Y);
            SetValue(LabelPointProperty, defaultLabel);

            DefaultFillOpacity = 1;
        }

        #endregion
    }
}
