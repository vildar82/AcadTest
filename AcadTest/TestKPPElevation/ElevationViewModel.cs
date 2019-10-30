using NetLib.WPF;

// ReSharper disable once CheckNamespace
namespace AcadTest
{
    public class DesignElevationViewModel : ElevationViewModel
    {
        public DesignElevationViewModel() : base(15)
        {
        }
    }

    public class ElevationViewModel : BaseViewModel
    {
        public int Elevation { get; set; }

        public ElevationViewModel(int elevation)
        {
            Elevation = elevation;
        }
    }
}