﻿/** 
 * Copyright (C) 2017 smndtrl, langboost, golf1052
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace libsignal.ecc.impl
{
	class Curve25519NativeProvider : ICurve25519Provider
	{
		//private curve25519.Curve25519Native native = new curve25519.Curve25519Native();

		public byte[] calculateAgreement(byte[] ourPrivate, byte[] theirPublic)
		{
            throw new NotImplementedException();
        }

		public byte[] calculateSignature(byte[] random, byte[] privateKey, byte[] message)
		{
            throw new NotImplementedException();
        }

        public byte[] calculateVrfSignature(byte[] privateKey, byte[] message)
        {
            throw new NotImplementedException();
        }

        public byte[] generatePrivateKey(byte[] random)
		{
            throw new NotImplementedException();
        }

		public byte[] generatePublicKey(byte[] privateKey)
		{
            throw new NotImplementedException();
        }

		public bool isNative()
		{
            throw new NotImplementedException();
        }

		public bool verifySignature(byte[] publicKey, byte[] message, byte[] signature)
		{
            throw new NotImplementedException();
        }

        public byte[] verifyVrfSignature(byte[] publicKey, byte[] message, byte[] signature)
        {
            throw new NotImplementedException();
        }
    }
}
