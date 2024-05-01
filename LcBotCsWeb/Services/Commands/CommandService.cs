﻿using System.Text.RegularExpressions;
using PsimCsLib.Entities;
using PsimCsLib.Enums;
using PsimCsLib.Models;
using PsimCsLib.PubSub;

namespace LcBotCsWeb.Services.Commands;

public class CommandService : ISubscriber<PrivateMessage>, ISubscriber<ChatMessage>
{
	private class HelpCommand : ICommand
	{
		private readonly CommandService _commandService;
		public List<string> Aliases => new List<string> { "help", "?" };
		public string HelpText => "Displays help text for any provided command";
		public Rank RequiredPublicRank => Rank.Voice;
		public bool AllowPublic => true;
		public Rank RequiredPrivateRank => Rank.Normal;
		public bool AllowPrivate => true;
		public bool AcceptIntro => false;

		public HelpCommand(CommandService commandService)
		{
			_commandService = commandService;
		}

		public async IAsyncEnumerable<string> Execute(DateTime timePosted, PsimUsername user, Room? room, List<string> arguments)
		{
			foreach (var arg in arguments)
			{
				if (_commandService.TryParseCommand(arg, out var command, out var _))
					yield return command.HelpText;
			}
		}
	}

	private readonly CommandOptions _options;
	private readonly List<ICommand> _commands;

	public CommandService(IServiceScopeFactory scopeFactory, CommandOptions options)
	{
		_options = options;
		_commands = new List<ICommand> { new HelpCommand(this) };

		var scope = scopeFactory.CreateScope();
		var modules = scope.ServiceProvider.GetServices<ICommand>();

		foreach (var module in modules)
			_commands.Add(module);
	}
	
	public async Task HandleEvent(PrivateMessage e) => await HandleCommand(e.IsIntro, e.Message, DateTime.UtcNow, e.Sender, null);
	public async Task HandleEvent(ChatMessage e) => await HandleCommand(e.IsIntro, e.Message, e.DatePosted, e.User, e.Room);
	private async Task HandleCommand(bool isIntro, string input, DateTime timePosted, PsimUsername user, Room? room)
	{
		if (!IsCommand(input))
			return;

		if (!TryParseCommand(input, out var command, out var parameters))
			return;

		if (!command.AcceptIntro && isIntro)
			return;

		var publicAuth = IsAuthorised(command.RequiredPublicRank, user.Rank);
		var privateAuth = IsAuthorised(command.RequiredPrivateRank, user.Rank);

		if (!publicAuth && !privateAuth) 
			return;

		var isPrivate = room == null || !publicAuth;

		switch (isPrivate)
		{
			case false when !command.AllowPublic:
			case true when !command.AllowPrivate:
				return;
		}

		async Task SendMessage(string message)
		{
			if (isPrivate)
				await user.Send(message);
			else
				await room?.Send(message);
		}

		try
		{
			var responses = await command.Execute(timePosted, user, room, parameters).ToListAsync();
			foreach (var response in responses)
				await SendMessage(response);
		}
		catch (Exception ex)
		{
			await SendMessage("An error occurred processing your command.");
			throw;
		}
	}

	private bool IsCommand(string input) => input.StartsWith(_options.CommandString);
	private List<string> BreakMessage(string input) => Regex.Split(input, @",(?=(?:(?:[^""]*""){2})*[^""]*$)").ToList();
	private bool TryParseCommand(string input, out ICommand command, out List<string> parameters)
	{
		var split = input.Split(' ', 2);
		var commandString = split[0].Replace(_options.CommandString, string.Empty).Trim().ToLowerInvariant();
		command = _commands.FirstOrDefault(c => c.Aliases.Contains(commandString))!;
		parameters = split.Length == 2 ? BreakMessage(split[1]) : new List<string>();
		return (command != null);
	}

	private bool IsAuthorised(Rank required, Rank current)
	{
		return current == required || current switch
		{
			Rank.Muted => IsAuthorised(required, Rank.Locked),
			Rank.Normal => IsAuthorised(required, Rank.Muted),
			Rank.Voice => IsAuthorised(required, Rank.Normal),
			Rank.Bot => IsAuthorised(required, Rank.Voice),
			Rank.Driver => IsAuthorised(required, Rank.Bot),
			Rank.Moderator => IsAuthorised(required, Rank.Driver),
			Rank.Administrator => IsAuthorised(required, Rank.Moderator),
			Rank.RoomOwner => IsAuthorised(required, Rank.Administrator),
			_ => false
		};
	}
}