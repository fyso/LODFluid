#ifndef CUSTOM_COMMONMATH_INCLUDED
#define CUSTOM_COMMONMATH_INCLUDED
#define MAX_DIMENSION 3
#define MAX_LOOP_ITERATIONS 20

float _Precision = 0.0001;

float SmoothKernel(float3 direction, float smoothRadius)
{
    float distance = length(direction);
    if (distance <= smoothRadius)
    {
        float weight = distance / smoothRadius;
        float k = 1 - weight * weight;
        return k * k * k;
    }
    return 0.0;
}

float SmoothKernelAnisotropic(float3 radialVec, float3x3 anisotropicMat)
{
    float det = determinant(anisotropicMat);
    float dis = length(mul(anisotropicMat, radialVec));
    //TODO:dis is [0,1]
    //dis = clamp(dis, 0, 1);
    float p = 1 - (6 * pow(dis, 5) - 15 * pow(dis, 4) + 10 * pow(dis, 3));
   
    return det * p;
}

float SmoothKernelNormal(float distance, float smoothRadius)
{
    if (distance <= smoothRadius)
    {
        float weight = distance / smoothRadius;
        return max(1 - weight * weight * weight, 0.0f);
    }
    return 0.0f;
}

bool checkSymmetricMatrix(float mat[MAX_DIMENSION][MAX_DIMENSION])
{
    for (uint i = 0; i < MAX_DIMENSION - 1; ++i)
    {
        for (uint k = i + 1; k < MAX_DIMENSION; ++k)
        {
            if (mat[i][k] != mat[k][i])
                return false;
        }
    }
    return true;
}

bool calculateSymmetricMatrixEigen_Jacobi(float mat[MAX_DIMENSION][MAX_DIMENSION], inout float eigenValue[MAX_DIMENSION], inout float eignVector2D[MAX_DIMENSION][MAX_DIMENSION], uint numIteration)
{
    //Check is SymmetricMatrix
    if (!checkSymmetricMatrix(mat))
        return false;
    
    float eignVectors[MAX_DIMENSION][MAX_DIMENSION];
    for (uint i = 0; i < MAX_DIMENSION; ++i)
    {
        for (uint k = 0; k < MAX_DIMENSION; ++k)
        {
            if (i == k)
                eignVectors[i][k] = 1.0f;
            else
                eignVectors[i][k] = 0.0f;
        }
    }

    uint countIter = 0;
    uint iterationCount = min(numIteration, MAX_LOOP_ITERATIONS);
    for (uint iter = 0; iter < iterationCount; ++iter)
    {
        uint nRow = 0;
        uint nCol = 0;
        float dbMax = 0;
        //Get max element row  and column
        for (uint i = 0; i < MAX_DIMENSION - 1; ++i)
        {
            for (uint k = i + 1; k < MAX_DIMENSION; ++k)
            {
                float d = abs(mat[i][k]);
                if (dbMax < d)
                {
                    dbMax = d;
                    nRow = i;
                    nCol = k;
                }
            }
        }
        if (dbMax < _Precision)
            break;
        if (countIter > numIteration)
            break;
        
        countIter++;
        
        //Calculate Rotation Angle
        float dbApp = mat[nRow][nRow];
        float dbAqq = mat[nCol][nCol];
        float dbApq = mat[nRow][nCol];
        float dbAngle = 0.5f * atan2(-2 * dbApq, dbAqq - dbApp);
        float dbSin = sin(dbAngle);
        float dbCos = cos(dbAngle);
        float dbSin2 = sin(2 * dbAngle);
        float dbCos2 = cos(2 * dbAngle);
        
        mat[nRow][nRow] = dbCos * dbCos * dbApp + 2 * dbSin * dbCos * dbApq + dbSin * dbSin * dbAqq;
        mat[nCol][nCol] = dbSin * dbSin * dbApp - 2 * dbSin * dbCos * dbApq + dbCos * dbCos * dbAqq;
        mat[nRow][nCol] = 0.5 * (dbAqq - dbApp) * dbSin2 + dbApq * dbCos2;
        mat[nCol][nRow] = mat[nRow][nCol];
        
        for (uint j = 0; j < MAX_DIMENSION; j++)
        {
            if (j != nRow && j != nCol)
            {
                dbMax = mat[nRow][j];
                float dbTemp = mat[j][nCol];
                mat[nRow][j] = dbMax * dbCos + dbTemp * dbSin; //p
                mat[j][nRow] = mat[nRow][j];
                
                mat[j][nCol] = dbTemp * dbCos - dbMax * dbSin; //q
                mat[nCol][j] = mat[j][nCol];
            }
        }
        
        //Calculate EignvectorsMatrix
        for (uint n = 0; n < MAX_DIMENSION; n++)
        {
            dbMax = eignVectors[n][nRow];
            float dbTemp = eignVectors[n][nCol];
            eignVectors[n][nRow] = dbMax * dbCos + dbTemp * dbSin; //p
            eignVectors[n][nCol] = dbTemp * dbCos - dbMax * dbSin; //q
        }
    }
     
    //Store EignValue: Descending
    uint maxIndex;
    float temp;
    uint indexArr[MAX_DIMENSION];
    for (uint index =0; index < MAX_DIMENSION; ++index)
    {
        eigenValue[index] = mat[index][index];
        indexArr[index] = index;
    }
    
    for (uint s = 0; s < MAX_DIMENSION - 1; s++)
    {
        maxIndex = s;
        for (uint t = s+1; t < MAX_DIMENSION; t++)
        {
            if (eigenValue[t] > eigenValue[maxIndex])
                maxIndex = t;
        }
        
        temp = eigenValue[s];
        eigenValue[s] = eigenValue[maxIndex];
        eigenValue[maxIndex] = temp;
        
        uint tempIndex = indexArr[s];
        indexArr[s] = indexArr[maxIndex];
        indexArr[maxIndex] = tempIndex;
    }
    
    //EignVector:column
    for (uint m = 0; m < MAX_DIMENSION; ++m)
    {   
        for (uint n = 0; n < MAX_DIMENSION; ++n)
        {
            eignVector2D[n][m] = eignVectors[n][indexArr[m]];
        }
    }
    return true;
}
#endif