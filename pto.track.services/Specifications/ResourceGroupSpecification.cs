using System;
using System.Linq.Expressions;
using pto.track.data;

namespace pto.track.services.Specifications
{
    public class ResourceGroupSpecification : BaseSpecification<Resource>
    {
        public ResourceGroupSpecification(int groupId)
            : base(r => r.GroupId == groupId)
        {
        }
    }
}
