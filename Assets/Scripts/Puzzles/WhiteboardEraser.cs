using UnityEngine;
using Fusion;
using Fusion.Addons.Drawing;

namespace EscapeRoom.Puzzles
{
    public class WhiteboardEraser : NetworkBehaviour
    {
        public Board targetBoard;

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_ClearBoard()
        {
            // Find all Draw objects in the scene
            Draw[] allDraws = UnityEngine.Object.FindObjectsByType<Draw>(FindObjectsSortMode.None);
            foreach (var draw in allDraws)
            {
                if (draw.ProjectionBoard == targetBoard)
                {
                    // Only the authority can despawn the object
                    if (draw.Object != null && draw.Object.HasStateAuthority)
                    {
                        Runner.Despawn(draw.Object);
                    }
                }
            }
            
            // Refresh the board texture
            if (targetBoard != null)
            {
                targetBoard.ActivateBoard();
            }
        }

        public void Erase()
        {
            RPC_ClearBoard();
        }
    }
}
