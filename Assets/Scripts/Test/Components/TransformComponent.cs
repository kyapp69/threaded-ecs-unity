using ECS.Storage;
using Utils;

namespace Test.Components
{
    public struct TransformComponent : IComponent
    {
        public Float3x4 Matrix;

		public TransformComponent(Float3x4 matrix)
		{
			Matrix = matrix;
		}
    }
}