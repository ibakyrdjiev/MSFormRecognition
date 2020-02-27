
namespace CustomVision.BindingModels
{
    class ImageLabel
    {
        public string TagName { get; set; }

        public decimal Top { get; set; }

        public decimal Left { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }
    }
}
