﻿using ApplicationCore.Chat.Domain;
using ApplicationCore.Exceptions;
using ApplicationCore.Interfaces;
using ApplicationCore.Users.Selectors;
using FluentValidation;
using MediatR;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationCore.Chat.Commands
{
    public class SendMessageCommand : IRequest<SendMessageCommandResult>
    {
        public string Content { get; set; }
        public ClaimsPrincipal User { get; set; }
    }

    public class SendMessageCommandResult
    {
        public int Id { get; set; }
    }

    public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageCommandValidator()
        {
            this.RuleFor(m => m.Content).NotNull().NotEmpty().MaximumLength(100);
        }
    }

    public class SendMessageCommandHandler
        : IRequestHandler<SendMessageCommand, SendMessageCommandResult>
    {
        private readonly IAppDbContext dbContext;
        private readonly IBotService botService;

        public SendMessageCommandHandler(IAppDbContext dbContext, IBotService botService)
        {
            this.dbContext = dbContext;
            this.botService = botService;
        }

        public async Task<SendMessageCommandResult> Handle(
            SendMessageCommand request,
            CancellationToken cancellationToken)
        {
            var applicationUser = await this.dbContext.Users.FromClaims(
                request.User,
                cancellationToken);

            if (applicationUser == null)
            {
                throw new UnAuthorizedException();
            }

            var msg = new Message(request.Content, applicationUser);

            if (msg.IsCommand())
            {
                await this.botService.SendCommand(msg.ToMessageCommand(), cancellationToken);
                return new SendMessageCommandResult { Id = -1 };
            }

            this.dbContext.Messages.Add(msg);
            await this.dbContext.SaveChangesAsync(cancellationToken);

            return new SendMessageCommandResult
            {
                Id = msg.MessageId
            };
        }
    }
}
