/* GaussJordanElimination.cs
 * Joel Hensley
 * January 16, 2010
 * This class is used to perform the Gauss-Jordan elimination
 * method on a matrix.
 */
namespace RatingSystem
{
    class GaussJordanElimination
    {
        /// <summary>
        /// Switches two rows in a matrix
        /// </summary>
        private void SwitchRows(double[][] matrix, int row1, int row2)
        {
            double[] temp;
            temp = matrix[row1];
            matrix[row1] = matrix[row2];
            matrix[row2] = temp;
        }

        /// <summary>
        /// Performs Gauss-Jordan elimination on the provided matrix
        /// </summary>
        public double[] PerformElimination(double[][] matrix, int rowCount,
                                           int colCount)
        {
            bool isLastRow;
            double temp;
            double[] solutions = new double[rowCount];

            for (int row = 0; row < rowCount; row++)
            {
                isLastRow = (row == (rowCount - 1));

                if (isLastRow && matrix[row][row] == 0)
                {
                    return null;
                }
                else if (matrix[row][row] == 0)
                {
                    SwitchRows(matrix, row, row + 1);
                }

                if (matrix[row][row] == 0.0)
                {
                    return null;
                }
                else
                {
                    temp = matrix[row][row];
                    for (int col = 0; col < colCount; col++)
                    {
                        matrix[row][col] /= temp;
                    }
                }

                for (int rowsAhead = row + 1; rowsAhead < rowCount; rowsAhead++)
                {
                    temp = matrix[rowsAhead][row];
                    for (int col = 0; col < colCount; col++)
                    {
                        matrix[rowsAhead][col] -= temp * matrix[row][col];
                    }
                }
            }

            for (int row = rowCount - 1; row >= 0; row--)
            {
                for (int rowsBehind = row - 1; rowsBehind >= 0; rowsBehind--)
                {
                    temp = matrix[rowsBehind][row];
                    for (int col = 0; col < colCount; col++)
                    {
                        matrix[rowsBehind][col] -= temp * matrix[row][col];
                    }
                }
            }

            for (int row = 0; row < rowCount; row++)
            {
                solutions[row] = matrix[row][colCount - 1];
            }

            return solutions;
        }
    }
}
