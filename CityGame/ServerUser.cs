using System;
using System.ServiceModel;

namespace CityGame
{
    public class ServerUser
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public OperationContext operationContext { get; set; }
        
    }
}
