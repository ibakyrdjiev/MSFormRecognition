using System.Drawing;

namespace ComputerVision.Models
{
    public class Position
    {
        public Point TopLeft { get; set; }

        public Point TopRight { get; set; }

        public Point BottomRight { get; set; }

        public Point BottomLeft { get; set; }

        public Rectangle ReturnAsRectangle()
        {
            return new Rectangle((int)TopLeft.X, (int)TopLeft.Y, (int)(TopRight.X - TopLeft.X), (int)(BottomLeft.Y - TopLeft.Y));
        }
    }
}
