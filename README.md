Chat API .NET
===========

This is a fork from currently unsupported [mgp25/Chat-API-NET](https://github.com/mgp25/Chat-API-NET) project - WhatsApp API client library written in C#.

# Modifications
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
