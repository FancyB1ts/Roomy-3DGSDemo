using UnityEngine;

public class FurnitureDataInputHandler : MonoBehaviour
{
 
    [SerializeField] private FurnitureData parent;   // drag the root here

    public void setup(FurnitureData parent)
    {
        this.parent = parent;
    }
    private void OnMouseEnter() => parent.OnMouseEnter();
    private void OnMouseExit()  => parent.OnMouseExit();
    private void OnMouseOver()  => parent.OnMouseOver();
    private void OnMouseDown()  => parent.OnMouseDown();
    private void OnMouseUp()    => parent.OnMouseUp();
 
}
