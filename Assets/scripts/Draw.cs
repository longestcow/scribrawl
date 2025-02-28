using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Draw : MonoBehaviour
{   
    public Camera m_camera;
    public GameObject brush;

    public string templateName = "rect";
    public bool save = true;
    public int lineCap = 200;

    LineRenderer currentLineRenderer;
    GameObject previousDrawing;

    Vector2 lastPos;

    DollarRecognizer recog;
    GameManager manager;
    bool started = false;

    public player player;
    void Start()
    {

        
        manager = Camera.main.gameObject.GetComponent<GameManager>();
        recog = manager.dollar;
    }

    void Update()
    {
        if(!player.grabbed && !player.hovering)
            Drawing();
    }

    void Drawing() 
    {
        if (Input.GetKeyDown(KeyCode.Mouse0)){
            if(previousDrawing!=null) Destroy(previousDrawing);
            CreateBrush();
            started=true;
        }
        else if (Input.GetKey(KeyCode.Mouse0) && started){
            PointToMousePos();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0) && started){
            Vector3[] positions3 = new Vector3[currentLineRenderer.positionCount];
            currentLineRenderer.GetPositions(positions3);
            Vector2[] positions2 = System.Array.ConvertAll<Vector3, Vector2> (positions3, getV2fromV3);
            if(save) {
                string text = templateName+":";
                for (int i = 0; i<positions2.Length; i++){
                    text+=positions2[i].x+","+positions2[i].y+";";
                }
                text=text.Substring(0,text.Length-1);
                GUIUtility.systemCopyBuffer = text;
                print("copied to clipboard new "+templateName); //copy to clipboard
                manager.storeData(text, templateName);
                recog.SavePattern(templateName, positions2); // save for current instance as well
            }

            DollarRecognizer.Result result = recog.Recognize(positions2);
            if(result.Match==null)
                return;
            print(result.ToString());
            if(result.Score < 0.6f)
                return;
            if(result.Match.Name=="rect"){
                float minX = float.MaxValue, maxX = float.MinValue;
                float minY = float.MaxValue, maxY = float.MinValue;

                foreach(Vector2 point in positions2){
                    if (point.x < minX) minX = point.x;
                    if (point.x > maxX) maxX = point.x;
                    if (point.y < minY) minY = point.y;
                    if (point.y > maxY) maxY = point.y;
                }
                PolygonCollider2D polyCol = previousDrawing.AddComponent<PolygonCollider2D>();
                polyCol.points = new Vector2[] { // create a box collider basically
                    new Vector2(minX, minY),
                    new Vector2(maxX, minY),
                    new Vector2(maxX, maxY),
                    new Vector2(minX, maxY),
                };
                Rigidbody2D rb = previousDrawing.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.bodyType = RigidbodyType2D.Static;
                // rb.drag = manager.wallDrag;
                rb.mass = manager.wallMass;
                rb.angularDrag = manager.wallDrag;
                currentLineRenderer.enabled = false;
                manager.walls.Add(polyCol);
                previousDrawing.layer = LayerMask.NameToLayer("wall");
                previousDrawing = null;
            }


            if(result.Match.Name=="arrow"){
                float angle = result.Angle * -1;
                if(angle<0)angle=360+angle;
                print(angle); // get angle like unit circle

                List<Vector2> newPoints = new List<Vector2>(), sortedPoints;
                if((angle<=45 || angle>=315) || (angle>=135 && angle<=225))     // facing forward or backward
                    sortedPoints = positions2.OrderBy(p => p.x).ToList();
                else                                                            // facing up or down
                    sortedPoints = positions2.OrderBy(p => p.y).ToList();
                newPoints.Add(sortedPoints.First());
                newPoints.Add(sortedPoints.Last());
                newPoints.Add(sortedPoints.Last()+new Vector2(0.01f, 0.01f));

                PolygonCollider2D polyCol = previousDrawing.AddComponent<PolygonCollider2D>();
                polyCol.points = newPoints.ToArray();

                Rigidbody2D rb = previousDrawing.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.drag = manager.arrowDrag;
                rb.mass = manager.arrowMass;
                rb.freezeRotation = true;

                previousDrawing.transform.position = newPoints[0];
                currentLineRenderer.enabled = false;
                manager.arrows.Add(polyCol);
                previousDrawing.transform.position = Vector3.zero;
                rb.velocity = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * manager.arrowVelocity;
                previousDrawing.layer = LayerMask.NameToLayer("arrow");
                previousDrawing.AddComponent<arrow>();
                previousDrawing = null;
            }

            currentLineRenderer = null;
            started=false;
        }
        else  {
            currentLineRenderer = null;
        }


    }

    void CreateBrush() 
    {
        GameObject brushInstance = Instantiate(brush);
        currentLineRenderer = brushInstance.GetComponent<LineRenderer>();
        previousDrawing = brushInstance;
        Vector2 mousePos = m_camera.ScreenToWorldPoint(Input.mousePosition);

        currentLineRenderer.SetPosition(0, mousePos);
        currentLineRenderer.SetPosition(1, mousePos);

    }

    void AddAPoint(Vector2 pointPos) 
    {
        currentLineRenderer.positionCount++;
        int positionIndex = currentLineRenderer.positionCount - 1;
        currentLineRenderer.SetPosition(positionIndex, pointPos);

        float totalLength = 0;
        Vector3[] positions = new Vector3[currentLineRenderer.positionCount];
        currentLineRenderer.GetPositions(positions);
        for (var i = 1; i < currentLineRenderer.positionCount; i++) {
            totalLength += Vector3.Distance(positions[i - 1], positions[i]);
        }


        if(totalLength > lineCap) {
            float newLength = 0;
            List<Vector3> points = new List<Vector3>();
            for(int i = currentLineRenderer.positionCount-1; i>0; i--){
                points.Add(currentLineRenderer.GetPosition(i));
                newLength += Vector3.Distance(currentLineRenderer.GetPosition(i), currentLineRenderer.GetPosition(i-1));
                if(newLength > lineCap) 
                    break;
            }
            if(newLength <= lineCap)
                points.Add(currentLineRenderer.GetPosition(0));
            points.Reverse();
            currentLineRenderer.positionCount = points.Count;
            currentLineRenderer.SetPositions(points.ToArray());
        }


    }

    void PointToMousePos() 
    {
        Vector2 mousePos = m_camera.ScreenToWorldPoint(Input.mousePosition);
        if (lastPos != mousePos) 
        {
            AddAPoint(mousePos);
            lastPos = mousePos;
        }
    }


		
	public static Vector2 getV2fromV3 (Vector3 v3)
	{
		return new Vector2 (v3.x, v3.y);
	}

}