using System;
using System.Collections.Generic;
using System.Text;

namespace CustomVision.BindingModels
{
    class ImageLabelsData
    {
        public string FileName { get; set; }

        public int FileWidthPx { get; set; }

        public int FileHeighPX { get; set; }

        public List<ImageLabel> ImageLabels { get; set; } = new List<ImageLabel>();
    }
}
