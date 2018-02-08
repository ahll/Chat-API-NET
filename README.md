Chat API .NET
===========

This is a fork from currently unsupported [mgp25/Chat-API-NET](https://github.com/mgp25/Chat-API-NET) project - WhatsApp API client library written in C#.

# Modifications
- new configuration
- session of request user encrypt key
- fixed encoder/decoder
- register/login constants fixes
- small protocol fixes
- demo client with some small functionality (login, get phone presence, status, photo) added

Modifications were "translated" from [tgalal/yowsup](https://github.com/tgalal/yowsup) project - Python WhatsApp API client library.

Most important files in *tgalal/yowsup*:
- [WhatsApp constants](https://github.com/tgalal/yowsup/tree/master/yowsup/env)
- [encoder](https://github.com/tgalal/yowsup/blob/master/yowsup/layers/coder/encoder.py) and [decoder](https://github.com/tgalal/yowsup/blob/master/yowsup/layers/coder/decoder.py)
- [authentication layer](https://github.com/tgalal/yowsup/blob/master/yowsup/layers/auth/layer_authentication.py)
- [protocol dictionary](https://github.com/tgalal/yowsup/blob/master/yowsup/layers/coder/tokendictionary.py)

# WhatsAppInfoWeb
Web API (ASP.NET Core Web API using .NET Framework) application that receives information about phone number using WhatsApp API.

Application is for PoC purposes only.

## Input:

* phone


## Output (ContactInfo instance):

* phone registered state
* user id
* status text
* last seen timestamp
* profile photo file
