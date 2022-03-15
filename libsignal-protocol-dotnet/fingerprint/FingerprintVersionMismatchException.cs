/** 
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

namespace org.whispersystems.libsignal.fingerprint
{
    public class FingerprintVersionMismatchException : Exception
    {
        private readonly int theirVersion;
        private readonly int ourVersion;

        public FingerprintVersionMismatchException(int theirVersion, int ourVersion) : base()
        {
            this.theirVersion = theirVersion;
            this.ourVersion = ourVersion;
        }

        public int getTheirVersion()
        {
            return theirVersion;
        }

        public int getOurVersion()
        {
            return ourVersion;
        }
    }
}
