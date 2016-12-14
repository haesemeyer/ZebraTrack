
using ZebraTrack.Experiments;

namespace ZebraTrack.ViewModels
{
    /// <summary>
    /// Interface definition for experiment describing
    /// View Models
    /// </summary>
    interface IExperimentVM
    {
        /// <summary>
        /// Use user entered information to create experimental class
        /// </summary>
        /// <param name="name">The name of the experiment</param>
        /// <param name="fishID">The fish id for the experiment</param>
        /// <returns></returns>
        ExperimentBase CreateExperiment(string name, string fishID);
    }
}
