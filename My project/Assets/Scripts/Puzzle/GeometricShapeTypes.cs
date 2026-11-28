namespace ProjectRac.Puzzle
{
    public enum ShapeComponent2D
    {
        Circle,
        Square,
        Triangle
    }

    public enum GeometricShape3D
    {
        Sphere,   // Circle + Circle
        Cube,     // Square + Square
        Pyramid,  // Triangle + Triangle
        Cylinder, // Square + Circle
        Cone,     // Circle + Triangle
        Prism     // Square + Triangle
    }

    public static class ShapeRules
    {
        public static GeometricShape3D ResolveShape(ShapeComponent2D a, ShapeComponent2D b)
        {
            if (a == ShapeComponent2D.Circle && b == ShapeComponent2D.Circle) return GeometricShape3D.Sphere;
            if (a == ShapeComponent2D.Square && b == ShapeComponent2D.Square) return GeometricShape3D.Cube;
            if (a == ShapeComponent2D.Triangle && b == ShapeComponent2D.Triangle) return GeometricShape3D.Pyramid;

            bool hasCircle = a == ShapeComponent2D.Circle || b == ShapeComponent2D.Circle;
            bool hasSquare = a == ShapeComponent2D.Square || b == ShapeComponent2D.Square;
            bool hasTriangle = a == ShapeComponent2D.Triangle || b == ShapeComponent2D.Triangle;

            if (hasSquare && hasCircle) return GeometricShape3D.Cylinder;
            if (hasCircle && hasTriangle) return GeometricShape3D.Cone;
            return GeometricShape3D.Prism; // Square + Triangle
        }

        public static string GetShapeName(GeometricShape3D shape)
        {
            switch (shape)
            {
                case GeometricShape3D.Sphere: return "Esfera";
                case GeometricShape3D.Cube: return "Cubo";
                case GeometricShape3D.Pyramid: return "Piramide";
                case GeometricShape3D.Cylinder: return "Cilindro";
                case GeometricShape3D.Cone: return "Cone";
                case GeometricShape3D.Prism: return "Prisma";
                default: return shape.ToString();
            }
        }

        public static string GetComponentName(ShapeComponent2D comp)
        {
            switch (comp)
            {
                case ShapeComponent2D.Circle: return "Circulo";
                case ShapeComponent2D.Square: return "Quadrado";
                case ShapeComponent2D.Triangle: return "Triangulo";
                default: return comp.ToString();
            }
        }

        /// <summary>
        /// Invariant multiset of 6 components required to form:
        /// Pyramid (T+T), Cylinder (Q+C), Prism (Q+T).
        /// Total: 3 Triangles, 2 Squares, 1 Circle.
        /// Any random partition of these 6 components is guaranteed 100% solvable via swaps.
        /// </summary>
        public static ShapeComponent2D[] CreateSolutionComponentPool()
        {
            return new[]
            {
                ShapeComponent2D.Triangle,
                ShapeComponent2D.Triangle,
                ShapeComponent2D.Triangle,
                ShapeComponent2D.Square,
                ShapeComponent2D.Square,
                ShapeComponent2D.Circle
            };
        }
    }
}
