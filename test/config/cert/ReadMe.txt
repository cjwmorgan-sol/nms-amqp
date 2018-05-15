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

Files located under the <project_root_dir>\test\config\cert contain 
example files to configure a secure client-server connection with certificate
authentication. These files may be used to configure the SSL/TLS unit tests when
the broker and local host do not already have a configured secure connection.

The following described files were all generated using the
openssl tool and the java keytool. This files contains useful information about
the example files.

Example Test Suite certificates.

ca.crt
______ 
    the self signed certificate authority certificate. This certificate
    has a common name of "NMS Test". The full Subject in the certificate is:

        CN=NMS Test, O=Internet Widgits Pty Ltd, L=Ottawa, S=Ontario, C=CA

    ca.crt should be installed on the client system in the 'Trusted Root
    Certificate Authorities'.  For Windows platforms see 'https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/how-to-view-certificates-with-the-mmc-snap-in'.
    AMQPnetLite does not support alternate trust stores, you must edit the
    operating system trust store if you wish to have the client recognize
    the self-signed certificate in ca.crt as authoritative. 

    NOTE: if your broker is configured with a certificate that has been signed
    by a recognized trusted authority you do not need to do anything to 
    configure the client and ca.crt may be ignored.


ca.key
______
    The private key for the ca.crt file.  You may need this to configure
    some brokers.  It is not required for ActgiveMQ (see ActiveMQ
    configuration) which uses a 'Java KeyStore' which has been provided.

broker.crt
__________

    broker.crt is a server certificate file signed by the ca.crt authority. This
    certificate also has a common name of "NMS Test".  Depending on your broker, 
    you may need to install this certificate and the companion private key.
    ActiveMQ requires a 'keystore' which is discussed below.

broker.key 
__________
    The unencrypted private key for the broker.crt file.

client.crt
__________
    Tn example client certificate file that can be by the Test Suite.
    This certificate has a common name of "NMS test Client".

client.key
__________
    The unencrypted private key for the client.crt file.

KeyStore files.
nms_test_broker.p12
____________________

    PKSC12 the key store that contains the server identity files: 
        - broker.crt
        - broker.key. 
        
The password to the key store is "password".

nms_test_broker.jks
__________________
    Java keystore that contains the server identity files:
        - ia.crt 
        - ia.key.

The password to the key store is "password".

client_trust.jks
________________
    The keystore that a broker would use to trust a
    client certificate. This store contains the certificate client.crt. 
    This store has the password "password".

ACtiveMQ Configuration
______________________

    ActiveMQ must be configured with an 'sslContext' that identifies
    the 'keyStore' the the server certificate.  If client-certificates
    are in use, a 'trustStore' must also be configured.  The 
    'sslContext' should be configured in the the 
    'broker' object, typically immediately below the destinationPolicy, 
    for example:

    <sslContext>
        <sslContext
            keyStore="${activemq.conf}/nms_test_broker.jks" 
            keyStorePassword="password"
            trustStore="${activemq.conf}/client_trust.jks
            trustStorePassword="password"
        />
    </sslContext> <!-- END OF SSL Context -->

    The above example is correct, there is a sslContext nested inside
    sslContext.

    Also you must add a AMQP+SSL transport connector to the
    'trasnsportConnectors' object description in the ActiveMQ
    configuation file,  for example:

	<transportConnector name="amqp+ssl" uri="amqp+ssl://0.0.0.0:5673?maximumConnections=1000&amp;wireFormat.maxFrameSize=104857600" />

