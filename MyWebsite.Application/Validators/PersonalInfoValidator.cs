using FluentValidation;
using MyWebsite.Core.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWebsite.Application.Validators
{
    public class PersonalInfoValidator : AbstractValidator<PersonalInfo>
    {
        public PersonalInfoValidator()
        {
            RuleFor(x => x.Nombre).NotEmpty().WithMessage("Nombre requerido");
            RuleFor(x => x.Apellido).NotEmpty().WithMessage("Apellido requerido");
            RuleFor(x => x.FechaNacimiento).NotEmpty().WithMessage("Fecha de nacimiento requerida");
        }
    }
}
