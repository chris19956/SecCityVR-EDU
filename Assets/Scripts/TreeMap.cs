using System;
using System.Collections.Generic;
using System.Linq;

public class LayoutRectangle
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Length { get; set; }

    public LayoutRectangle(float x, float y, float width, float length)
    {
        X = x;
        Y = y;
        Width = width;
        Length = length;
    }

}

// https://swharden.com/blog/2023-03-07-treemapping/
public static class TreeMap
{

    public static LayoutRectangle[] GetRectangles(double[] values, double width, double height)
    {
        if (values == null || values.Length == 0) return Array.Empty<LayoutRectangle>();
        if (width <= 0 || height <= 0 || double.IsNaN(width) || double.IsNaN(height) ||
            double.IsInfinity(width) || double.IsInfinity(height))
            throw new ArgumentException("Treemap dimensions must be finite and positive.");
        if (values.Any(v => v <= 0 || double.IsNaN(v) || double.IsInfinity(v)))
            throw new ArgumentException("Treemap weights must be finite and positive.");
        if (values.Length == 1)
            return new[] { new LayoutRectangle(0, 0, (float)width, (float)height) };
        for (int i = 1; i < values.Length; i++)
            if (values[i] > values[i - 1])
                throw new ArgumentException("values must be ordered large to small");

        Slice slice = GetSlice(values, 1, 0.35f);
        IEnumerable<SliceRectangle> rectangles = GetRectangles(slice, width, height);
        return rectangles.Select(x => x.ToBuildingRectangle()).ToArray();
    }

    private class Slice
    {
        public double Size { get; }
        public IEnumerable<double> Values { get; }
        public Slice[] Children { get; }

        public Slice(double size, IEnumerable<double> values, Slice sub1, Slice sub2)
        {
            Size = size;
            Values = values;
            Children = new Slice[] { sub1, sub2 };
        }

        public Slice(double size, double finalValue)
        {
            Size = size;
            Values = new double[] { finalValue };
            Children = Array.Empty<Slice>();
        }
    }

    private class SliceResult
    {
        public double ElementsSize { get; }
        public IEnumerable<double> Elements { get; }
        public IEnumerable<double> RemainingElements { get; }

        public SliceResult(double elementsSize, IEnumerable<double> elements, IEnumerable<double> remainingElements)
        {
            ElementsSize = elementsSize;
            Elements = elements;
            RemainingElements = remainingElements;
        }
    }

    private class SliceRectangle
    {
        public Slice Slice { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public SliceRectangle(Slice slice) => Slice = slice;
        public LayoutRectangle ToBuildingRectangle() => new(X, Y, Width, Height);
    }



    private static Slice GetSlice(IEnumerable<double> elements, double totalSize, double sliceWidth)
    {
        if (elements.Count() == 1)
            return new Slice(totalSize, elements.Single());

        SliceResult sr = GetElementsForSlice(elements, sliceWidth);

        // Check if elements are being correctly partitioned
        //if (!sr.Elements.Any() || !sr.RemainingElements.Any())
        //{
        //    throw new InvalidOperationException("GetElementsForSlice failed to partition the elements correctly.");
        //}

        Slice child1 = GetSlice(sr.Elements, sr.ElementsSize, sliceWidth);
        Slice child2 = GetSlice(sr.RemainingElements, 1 - sr.ElementsSize, sliceWidth);
        return new Slice(totalSize, elements, child1, child2);
    }

    private static SliceResult GetElementsForSlice(IEnumerable<double> elements, double sliceWidth)
    {
        List<double> elementsInSlice = new List<double>();
        List<double> remainingElements = new List<double>();
        double current = 0;
        double total = elements.Sum();

        foreach (double element in elements)
        {
            if (current > sliceWidth)
                remainingElements.Add(element);
            else
            {
                elementsInSlice.Add(element);
                current += element / total;
            }


        }

        if (!elementsInSlice.Any())
        {
            elementsInSlice.Add(remainingElements.First());
            remainingElements.RemoveAt(0);
        }
        else if (!remainingElements.Any())
        {
            remainingElements.Add(elementsInSlice.Last());
            elementsInSlice.RemoveAt(elementsInSlice.Count - 1);
        }
        return new SliceResult(elementsInSlice.Sum() / total, elementsInSlice, remainingElements);
    }

    private static IEnumerable<SliceRectangle> GetRectangles(Slice slice, double width, double height)
    {
        SliceRectangle area = new(slice) { Width = (float)width, Height = (float)height };

        foreach (SliceRectangle rect in GetRectangles(area))
        {
            if (rect.X + rect.Width > area.Width)
                rect.Width = area.Width - rect.X;

            if (rect.Y + rect.Height > area.Height)
                rect.Height = area.Height - rect.Y;

            yield return rect;
        }
    }

    private static IEnumerable<SliceRectangle> GetRectangles(SliceRectangle sliceRectangle)
    {
        bool isHorizontalSplit = sliceRectangle.Width >= sliceRectangle.Height;
        double currentPos = 0;
        foreach (Slice subSlice in sliceRectangle.Slice.Children)
        {
            SliceRectangle subRect = new SliceRectangle(subSlice);
            double rectSize;

            if (isHorizontalSplit)
            {
                rectSize = sliceRectangle.Width * subSlice.Size;
                subRect.X = sliceRectangle.X + (float)currentPos;
                subRect.Y = sliceRectangle.Y;
                subRect.Width = (float)rectSize;
                subRect.Height = sliceRectangle.Height;
            }
            else
            {
                rectSize = sliceRectangle.Height * subSlice.Size;
                subRect.X = sliceRectangle.X;
                subRect.Y = sliceRectangle.Y + (float)currentPos;
                subRect.Width = sliceRectangle.Width;
                subRect.Height = (float)rectSize;
            }

            currentPos += rectSize;

            if (subSlice.Values.Count() > 1)
            {
                foreach (SliceRectangle sr in GetRectangles(subRect))
                {
                    yield return sr;
                }
            }
            else if (subSlice.Values.Count() == 1)
            {
                yield return subRect;
            }
        }
    }
}

