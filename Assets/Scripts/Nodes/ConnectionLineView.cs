namespace Nodia.Nodes
{
    // Identifies one drawn connection line so it can be Shift+clicked directly
    // to delete it, instead of having to re-select both of its endpoint nodes.
    public class ConnectionLineView : UnityEngine.MonoBehaviour
    {
        public string ConnectionId { get; set; }
        public (string from, string to) PairKey { get; set; }
    }
}
