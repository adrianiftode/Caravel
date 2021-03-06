using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caravel.Errors;
using FluentValidation;
using MediatR;
using ValidationException = Caravel.Exceptions.ValidationException;

namespace Caravel.MediatR.Behaviours
{
    public class RequestValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public RequestValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<TResponse> next)
        {
            var context = new ValidationContext<TRequest>(request);

            var errors = _validators
                .Select(v => v.Validate(context))
                .SelectMany(result => result.Errors)
                .Where(e => e != null)
                .GroupBy(k => k.PropertyName, v => v)
                .ToDictionary(k => k.Key, v => v.Select(e => e.ErrorMessage).ToArray());
            
            if (errors.Any())
            {
                throw new ValidationException(new Error("invalid_fields", "Payload contains invalid fields."), errors);
            }

            return next();
        }
    }
}