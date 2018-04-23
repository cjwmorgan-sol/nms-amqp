# 
# Licensed to the Apache Software Foundation (ASF) under one or more
# contributor license agreements.  See the NOTICE file distributed with
# this work for additional information regarding copyright ownership.
# The ASF licenses this file to You under the Apache License, Version 2.0
# (the "License"); you may not use this file except in compliance with
# the Lic/dense.  You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#

All files located under the <project_root_dir>\test\config\cert contain 
example files to configure a secure client-server connection with certificate
authentication. The following described files were all generated using the
openssl tool and the java keytool. This files contains useful information about
the example files.

Example Test Suite certificates.

ca.crt is the self signed certificate authority certificate. This certificate
has a common name of "Test nms req".
ca.key is the private key for the ca.crt file.

ia.crt is the server certificate file signed by the ca.crt authority. This
certificate has a common name of "NMS Test".
ia.key is the unencrypted private key for the ia.crt file.

client.crt is an example client certificate file used by the Test Suite.
This certificate has a common name of "NMS test Client".
client.key is the unencrypted private key for the client.crt file.

KeyStore files.

identity.p12 is PKSC12 the key store that contains the server identity files: 
        - ia.crt
        - ia.key. 
        
The password to the key store is "password".

identity.jks is java keystore that contains the server identity files:
        - ia.crt 
        - ia.key.

The password to the key store is "password".

client_trust.jks is the keystore that a broker would use to trust a
client certificate. This store contains the certificate client.crt. 
This store has the password "password".

ACtiveMQ Configuration:
The easiest way to set ActiveMQ to use identity.jks is overwrite 'broker.ks' in the
ActiveMQ configuration directory.  Also be sure to add a AMQP+SSL transport connector 
to the ActiveMQ configuation file,  for example:
	transportConnector name="amqp+ssl" uri="amqp+ssl://0.0.0.0:5673?maximumConnections=1000&amp;wireFormat.maxFrameSize=104857600" />

