using System.Collections.Generic;
using UnityEngine;

public class LightningShapeGenerator : MonoBehaviour
{
    [Header("Shape Generation Settings: ")]
    [SerializeField, Range(0, 10)] private int mixGenerationsCount = 6;
    [SerializeField, Range(0, 10)] private int maxGenerationsCount = 8;
    [SerializeField, Range(0f, 1f)] private float nextGenerationSupportPercentage = 0.8f;
    
    [Header("Middle Point Displacement Settings: ")]
    [SerializeField, Range(0.00001f, 1f)] private float maxMiddlePointDisplacement = 0.2f;
    [SerializeField, Range(0f, 1f)] private float displacementDecreaseMultiplierByGeneration = 0.55f;
    
    [Header("New Branch Birth Settings: ")]
    [SerializeField, Range(0f, 1f)] private float newLightningBirthChance = 0.15f;
    [SerializeField, Range(0f, 2f)] private float birthChanceMultiplierByGeneration = 1.25f;
    [SerializeField, Range(1, 20)] private int maxBranchesCount = 10;
    [SerializeField, Range(0f, 1f)] private float newBranchIntensityDecreaseMultiplier = 0.45f;
    [SerializeField, Range(0f, 1f)] private float newBranchWidthDecreaseMultiplier = 0.8f;
    
    
    // Randomization control variables. 
    [SerializeField] private int currentRandomizationSeed = 0;
    private System.Random random = null;
    
    public List<LightningBranch> CreateLightningShape(Vector3 originPoint, Vector3 impactPoint)
    {
        // Initializing deterministic randomization. 
        random = new System.Random(currentRandomizationSeed);
        
        // Creating a new initial lightning bolt.
        LightningBranch initialLightningBranch = CreateInitialLightningBranch(originPoint, impactPoint);
        List<LightningBranch> lightningBranches = new List<LightningBranch>() { initialLightningBranch };

        // For every lighting branch, run the algorithm that displaces it's points until the max generations have been reached.
        // Important note, must calculate list count every iteration since new branches can be dynamically added to the collection. 
        for (int lightningBranchIndex = 0; lightningBranchIndex < lightningBranches.Count; lightningBranchIndex++)
        {
            LightningBranch lightningBranch = lightningBranches[lightningBranchIndex];
            GenerateLightningShape(lightningBranch, lightningBranches);
        }
        
        return lightningBranches;
    }
    

    private LightningBranch CreateInitialLightningBranch(Vector3 originPosition, Vector3 impactPosition)
    {
        Vector3 originPointLocalSpace = transform.InverseTransformPoint(originPosition);
        Vector3 impactPointLocalSpace = transform.InverseTransformPoint(impactPosition);
        
        // Initialize local point axis required for orientation calculation. 
        Vector3 forwardAxis = (impactPointLocalSpace - originPointLocalSpace).normalized;
        (Vector3 rightAxis, Vector3 upAxis) = CreateOrthogonalAxis(forwardAxis);
        
        // Create a collection for the lighting points
        List<LightningPoint> points = new List<LightningPoint>();
        
        // Add two initial points, an origin and an impact point.
        LightningPoint originPoint = new LightningPoint
        {
            Position = originPointLocalSpace,
            ForwardAxis = forwardAxis,
            RightAxis = rightAxis,
            UpAxis = upAxis,
            SupportsNextGenerations = true
        };
        LightningPoint impactPoint = new LightningPoint
        {
            Position = impactPointLocalSpace,
            ForwardAxis = forwardAxis,
            RightAxis = rightAxis,
            UpAxis = upAxis,
            SupportsNextGenerations = true
        };
        
        points.Add(originPoint);
        points.Add(impactPoint);
        
        // Creating initial lightning branch for the algorithm.
        LightningBranch initialLightningBranch = new LightningBranch
        {
            IntensityPercentage = 1f,
            WidthPercentage = 1f,
            CreationGeneration = 0,
            SpawnPointIndex = 0,
            LightningPoints = points
        };
        
        return initialLightningBranch;
    }
    
    private void GenerateLightningShape(LightningBranch lightningBranch, List<LightningBranch> lightningBranches)
    {
        int initialGeneration = lightningBranch.CreationGeneration;
        
        // The first branch can skip the first generation of the algorithm since we manually added two initial points (Origin and impact).
        bool isMainLightningBranch = lightningBranch.CreationGeneration == 0 && lightningBranch.SpawnPointIndex == 0;
        if (isMainLightningBranch)
        {
            initialGeneration += 1;
        }
        
        // Running the algorithm for splitting the lightning into segments and creating the shape of the lightning.
        for (int currentGeneration = 0; initialGeneration <= maxGenerationsCount; currentGeneration++)
        {
            // Every new generation should have a smaller maximum allowed offset than the previous one. 
            float generationMaxPossibleOffset = maxMiddlePointDisplacement * Mathf.Pow(displacementDecreaseMultiplierByGeneration, currentGeneration - 1);
            // Every new should have a higher chance of spawning supporting lightning bolts. 
            float newBranchCreationChance = newLightningBirthChance * Mathf.Pow(birthChanceMultiplierByGeneration, currentGeneration - 1);
            
            // Index defining the starting point of the algorithm. In most cases we start from the first point, except
            // when continuing the algorithm for the newly created supporting branches. 
            int firstPointIndex = currentGeneration == initialGeneration ? lightningBranch.SpawnPointIndex : 0;
            
            List<LightningPoint> points = lightningBranch.LightningPoints;
            // Must calculate the points count every iteration since new points are being added to the collection. 
            for (int pointIndex = firstPointIndex; pointIndex < (points.Count - 1); pointIndex++)
            {
                LightningPoint currentPoint = points[pointIndex];
                LightningPoint nextPoint = points[pointIndex + 1];
                
                // Calculate the mid point position. 
                Vector3 midPointPosition = Vector3.Lerp(currentPoint.Position, nextPoint.Position, 0.5f);
                
                // Calculate local axis for the middle point, upon which it will be displaced. 
                Vector3 pointForwardAxis = currentPoint.ForwardAxis;
                Vector3 pointRightAxis = currentPoint.RightAxis;
                Vector3 pointUpAxis = currentPoint.UpAxis;
                
                // Initialize the new middle point.
                LightningPoint newMidPoint = new LightningPoint
                {
                    Position = midPointPosition,
                    ForwardAxis = pointForwardAxis,
                    RightAxis = pointRightAxis,
                    UpAxis = pointUpAxis,
                    SupportsNextGenerations = true
                };
                
                // Offset mid point by random amount, limited by maximum allowed offset. 
                float randomRightOffset = Mathf.Lerp(- generationMaxPossibleOffset, generationMaxPossibleOffset, (float)random.NextDouble());
                float randomUpOffset = Mathf.Lerp(- generationMaxPossibleOffset, generationMaxPossibleOffset, (float)random.NextDouble());
                Vector3 rightAxisOffset = pointRightAxis * randomRightOffset;
                Vector3 upAxisOffset = pointUpAxis * randomUpOffset;
                
                // Displacing the middle point
                midPointPosition += rightAxisOffset + upAxisOffset;
                newMidPoint.Position = midPointPosition;
                
                // Update orientations of the current point to look at the newly added mid point. 
                pointForwardAxis = (midPointPosition - currentPoint.Position).normalized;
                (pointRightAxis, pointUpAxis) = CreateOrthogonalAxis(pointForwardAxis);
                currentPoint.ForwardAxis = pointForwardAxis;
                currentPoint.RightAxis = pointRightAxis;
                currentPoint.UpAxis = pointUpAxis;
                
                // Update orientation of the middle point to look at the next point. 
                pointForwardAxis = (nextPoint.Position - midPointPosition).normalized;
                (pointRightAxis, pointUpAxis) = CreateOrthogonalAxis(pointForwardAxis);
                newMidPoint.ForwardAxis = pointForwardAxis;
                newMidPoint.RightAxis = pointRightAxis;
                newMidPoint.UpAxis = pointUpAxis;
                
                // Adding middle point to the collection of points. 
                points.Insert(pointIndex + 1, newMidPoint);
                // Skipping over the newly added middle point for the next iteration of the loop. 
                pointIndex++;
                
                // Check if we can spawn new branch or not. Forbidding spawning new branches in the last generation. 
                if (currentGeneration == maxGenerationsCount || lightningBranches.Count == maxBranchesCount)
                {
                    continue;
                }
                
                bool shouldSpawnNewBranch = (float)random.NextDouble() <= newBranchCreationChance;
                if (shouldSpawnNewBranch)
                {
                    // Deep copy of the whole collection so that we would create new point instances. 
                    List<LightningPoint> newPoints = new List<LightningPoint>(points.Capacity);
                    for (int i = 0; i < points.Count; i++)
                    {
                        LightningPoint sourcePoint = points[i];
                        newPoints.Add( new LightningPoint
                        {
                            Position = sourcePoint.Position,
                            ForwardAxis = sourcePoint.ForwardAxis,
                            RightAxis = sourcePoint.RightAxis,
                            UpAxis = sourcePoint.UpAxis,
                            SupportsNextGenerations = sourcePoint.SupportsNextGenerations,
                        });
                    }
                    
                    // Create a new lighting branch and initialize it's starting properties. 
                    LightningBranch newLightningBranch = new LightningBranch
                    {
                        CreationGeneration = currentGeneration,
                        SpawnPointIndex = pointIndex + 1,
                        IntensityPercentage = lightningBranch.IntensityPercentage * newBranchIntensityDecreaseMultiplier,
                        WidthPercentage = lightningBranch.WidthPercentage * newBranchWidthDecreaseMultiplier,
                        LightningPoints = newPoints
                    };
                    
                    lightningBranches.Add(newLightningBranch);
                }
            }
        }

    }

    protected virtual (Vector3, Vector3) CreateOrthogonalAxis(Vector3 forwardAxis)
    {
        Vector3 rightAxis;
        if (Mathf.Approximately(Mathf.Abs(forwardAxis.y), 1f))
        {
            // When looking straight up/down.
            rightAxis = forwardAxis.y > 0f ? Vector3.right : Vector3.left;
        }
        else
        {
            rightAxis = Vector3.Cross(Vector3.up, forwardAxis).normalized;
        }
        Vector3 upAxis = Vector3.Cross(forwardAxis, rightAxis);
        return (rightAxis, upAxis);
    }
}
